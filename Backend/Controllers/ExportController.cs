using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MuafaPlus.Data;
using MuafaPlus.Models;
using MuafaPlus.Services;

namespace MuafaPlus.Controllers;

/// <summary>
/// Streams exported session files (PDF or Word) to the physician.
/// Enforces physician-scope: only the owning physician can export a session.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/sessions")]
public class ExportController : ControllerBase
{
    private readonly MuafaDbContext _db;
    private readonly ExportService  _export;
    private readonly ILogger<ExportController> _logger;

    public ExportController(MuafaDbContext db, ExportService export, ILogger<ExportController> logger)
    {
        _db     = db;
        _export = export;
        _logger = logger;
    }

    /// <summary>
    /// Export all articles in a session as PDF or Word.
    /// GET /api/v1/sessions/{id}/export?format=pdf
    /// GET /api/v1/sessions/{id}/export?format=docx
    /// </summary>
    [HttpGet("{id}/export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Export(string id, [FromQuery] string format = "pdf")
    {
        var physicianId = User.FindFirst(ClaimNames.PhysicianId)?.Value;
        if (string.IsNullOrEmpty(physicianId)) return Unauthorized();

        if (format is not ("pdf" or "docx"))
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Error   = "Format must be 'pdf' or 'docx'."
            });

        // Load session with articles
        var session = await _db.GenerationSessions
            .Include(s => s.GeneratedArticles)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SessionId == id);

        if (session == null)
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Error   = $"Session '{id}' not found."
            });

        if (session.PhysicianId != physicianId)
        {
            _logger.LogWarning("Physician {P} attempted to export session {S} owned by {O}",
                physicianId, id, session.PhysicianId);
            return Forbid();
        }

        if (session.Status != "complete")
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Error   = $"Session is not complete (current status: {session.Status}). Export is only available for completed sessions."
            });

        var exportData = new SessionExportData
        {
            SessionId = session.SessionId,
            RiskLevel = session.RiskLevel,
            Articles  = session.GeneratedArticles
                .OrderBy(a => a.ArticleType == "summary" ? 0 : 1)
                .ThenBy(a => a.CreatedAt)
                .Select(a => new ArticleExportItem
                {
                    ArticleType   = a.ArticleType,
                    CoverageCodes = a.CoverageCodes ?? string.Empty,
                    Content       = a.Content
                })
                .ToList()
        };

        var shortId = id[..8];

        if (format == "pdf")
        {
            _logger.LogInformation("Exporting PDF — session:{Id}", id);
            var pdfBytes = _export.GenerateSessionPdf(exportData);
            return File(pdfBytes,
                "application/pdf",
                $"muafaplus_{shortId}.pdf");
        }
        else
        {
            _logger.LogInformation("Exporting DOCX — session:{Id}", id);
            var docxBytes = _export.GenerateSessionDocx(exportData);
            return File(docxBytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                $"muafaplus_{shortId}.docx");
        }
    }
}
