using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MuafaPlus.Data;
using MuafaPlus.Models;
using MuafaPlus.Services;

namespace MuafaPlus.Controllers;

/// <summary>
/// Session retrieval and job-status polling.
/// All endpoints are physician-scoped — a physician can only access
/// sessions they own (PhysicianId claim from JWT).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class SessionController : ControllerBase
{
    private readonly MuafaDbContext _db;
    private readonly ILogger<SessionController> _logger;

    public SessionController(MuafaDbContext db, ILogger<SessionController> logger)
    {
        _db     = db;
        _logger = logger;
    }

    // ── GET /api/v1/Session/{id} ──────────────────────────────────────────────

    /// <summary>
    /// Returns full session data including all generated articles.
    /// Used by the frontend session viewer to load content from the database
    /// instead of relying on sessionStorage (Phase 3 upgrade).
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<SessionDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<SessionDetailDto>>> GetById(string id)
    {
        var physicianId = User.FindFirst(ClaimNames.PhysicianId)?.Value;
        if (string.IsNullOrEmpty(physicianId)) return Unauthorized();

        var session = await _db.GenerationSessions
            .Include(s => s.GeneratedArticles)
            .FirstOrDefaultAsync(s => s.SessionId == id);

        if (session == null)
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Error   = $"Session '{id}' not found."
            });

        // Physician scope enforcement
        if (session.PhysicianId != physicianId)
        {
            _logger.LogWarning("Physician {P} attempted to access session {S} owned by {O}",
                physicianId, id, session.PhysicianId);
            return Forbid();
        }

        var articles = session.GeneratedArticles
            .OrderBy(a => a.ArticleType == "summary" ? 0 : 1)
            .ThenBy(a => a.CreatedAt)
            .Select(a => new ArticleDto
            {
                ArticleId    = a.ArticleId,
                ArticleType  = a.ArticleType,
                CoverageCodes = a.CoverageCodes ?? string.Empty,
                Content      = a.Content,
                WordCount    = a.WordCount,
                CostUsd      = a.CostUsd,
                CreatedAt    = a.CreatedAt
            })
            .ToList();

        return Ok(new ApiResponse<SessionDetailDto>
        {
            Success = true,
            Data    = new SessionDetailDto
            {
                SessionId     = session.SessionId,
                PatientId     = session.PatientId,
                PhysicianId   = session.PhysicianId,
                Stage         = session.Stage,
                Status        = session.Status,
                RiskLevel     = session.RiskLevel,
                TotalArticles = session.TotalArticles,
                TotalCost     = session.TotalCost,
                StartedAt     = session.StartedAt,
                CompletedAt   = session.CompletedAt,
                ErrorMessage  = session.ErrorMessage,
                Articles      = articles
            }
        });
    }

    // ── GET /api/v1/Session/{id}/status ───────────────────────────────────────

    /// <summary>
    /// Lightweight polling endpoint — returns only status, risk level, and article count.
    /// The frontend polls this every 3 seconds while a Hangfire job is running.
    /// </summary>
    [HttpGet("{id}/status")]
    [ProducesResponseType(typeof(ApiResponse<SessionStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SessionStatusDto>>> GetStatus(string id)
    {
        var physicianId = User.FindFirst(ClaimNames.PhysicianId)?.Value;
        if (string.IsNullOrEmpty(physicianId)) return Unauthorized();

        var session = await _db.GenerationSessions
            .AsNoTracking()
            .Where(s => s.SessionId == id && s.PhysicianId == physicianId)
            .Select(s => new SessionStatusDto
            {
                SessionId     = s.SessionId,
                Status        = s.Status,
                RiskLevel     = s.RiskLevel,
                TotalArticles = s.TotalArticles,
                TotalCost     = s.TotalCost,
                CompletedAt   = s.CompletedAt,
                ErrorMessage  = s.ErrorMessage
            })
            .FirstOrDefaultAsync();

        if (session == null)
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Error   = $"Session '{id}' not found."
            });

        return Ok(new ApiResponse<SessionStatusDto> { Success = true, Data = session });
    }
}

// ── Response DTOs ─────────────────────────────────────────────────────────────

public class SessionDetailDto
{
    public string    SessionId     { get; set; } = string.Empty;
    public string    PatientId     { get; set; } = string.Empty;
    public string    PhysicianId   { get; set; } = string.Empty;
    public string    Stage         { get; set; } = string.Empty;
    public string    Status        { get; set; } = string.Empty;
    public string?   RiskLevel     { get; set; }
    public int?      TotalArticles { get; set; }
    public decimal?  TotalCost     { get; set; }
    public DateTime  StartedAt     { get; set; }
    public DateTime? CompletedAt   { get; set; }
    public string?   ErrorMessage  { get; set; }
    public List<ArticleDto> Articles { get; set; } = [];
}

public class SessionStatusDto
{
    public string    SessionId     { get; set; } = string.Empty;
    public string    Status        { get; set; } = string.Empty;
    public string?   RiskLevel     { get; set; }
    public int?      TotalArticles { get; set; }
    public decimal?  TotalCost     { get; set; }
    public DateTime? CompletedAt   { get; set; }
    public string?   ErrorMessage  { get; set; }
}

public class ArticleDto
{
    public string   ArticleId     { get; set; } = string.Empty;
    public string   ArticleType   { get; set; } = string.Empty;
    public string   CoverageCodes { get; set; } = string.Empty;
    public string   Content       { get; set; } = string.Empty;
    public int      WordCount     { get; set; }
    public decimal  CostUsd       { get; set; }
    public DateTime CreatedAt     { get; set; }
}
