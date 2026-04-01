using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MuafaPlus.Data;
using MuafaPlus.Models;

namespace MuafaPlus.Controllers;

/// <summary>
/// CRUD endpoints for physician profiles.
/// This controller was absent in the original prototype despite the Physician
/// model and PhysicianCreateDto existing in the codebase.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class PhysicianController : ControllerBase
{
    private readonly MuafaDbContext _db;
    private readonly ILogger<PhysicianController> _logger;

    public PhysicianController(MuafaDbContext db, ILogger<PhysicianController> logger)
    {
        _db     = db;
        _logger = logger;
    }

    // ── GET /api/v1/Physician ─────────────────────────────────────────────────

    /// <summary>Returns all active physicians.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PhysicianSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<PhysicianSummaryDto>>>> GetAll()
    {
        var physicians = await _db.Physicians
            .Where(p => p.IsActive)
            .OrderBy(p => p.FullName)
            .Select(p => new PhysicianSummaryDto
            {
                PhysicianId  = p.PhysicianId,
                FullName     = p.FullName,
                Specialty    = p.Specialty,
                Institution  = p.Institution,
                City         = p.City,
                TotalSessions = p.GenerationSessions.Count
            })
            .ToListAsync();

        return Ok(new ApiResponse<IEnumerable<PhysicianSummaryDto>>
        {
            Success = true,
            Data    = physicians,
            Metadata = new Dictionary<string, object>
            {
                ["count"] = physicians.Count
            }
        });
    }

    // ── GET /api/v1/Physician/{id} ────────────────────────────────────────────

    /// <summary>Returns a single physician profile by ID.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<Physician>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<Physician>>> GetById(string id)
    {
        var physician = await _db.Physicians.FindAsync(id);

        if (physician == null || !physician.IsActive)
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Error   = $"Physician '{id}' not found."
            });

        return Ok(new ApiResponse<Physician> { Success = true, Data = physician });
    }

    // ── GET /api/v1/Physician/{id}/sessions ──────────────────────────────────

    /// <summary>Returns generation sessions for a physician, newest first.</summary>
    [HttpGet("{id}/sessions")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<SessionSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<SessionSummaryDto>>>> GetSessions(
        string id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (!await _db.Physicians.AnyAsync(p => p.PhysicianId == id && p.IsActive))
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Error   = $"Physician '{id}' not found."
            });

        pageSize = Math.Clamp(pageSize, 1, 100);

        var sessions = await _db.GenerationSessions
            .Where(s => s.PhysicianId == id)
            .OrderByDescending(s => s.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SessionSummaryDto
            {
                SessionId     = s.SessionId,
                PatientId     = s.PatientId,
                Status        = s.Status,
                RiskLevel     = s.RiskLevel,
                TotalArticles = s.TotalArticles,
                TotalCost     = s.TotalCost,
                StartedAt     = s.StartedAt,
                CompletedAt   = s.CompletedAt
            })
            .ToListAsync();

        return Ok(new ApiResponse<IEnumerable<SessionSummaryDto>>
        {
            Success  = true,
            Data     = sessions,
            Metadata = new Dictionary<string, object>
            {
                ["page"]     = page,
                ["pageSize"] = pageSize,
                ["count"]    = sessions.Count
            }
        });
    }

    // ── POST /api/v1/Physician ────────────────────────────────────────────────

    /// <summary>Creates a new physician profile.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Physician>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<Physician>>> Create(
        [FromBody] PhysicianCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Error   = "Validation failed.",
                Metadata = new Dictionary<string, object>
                {
                    ["errors"] = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                }
            });

        // Unique license number check
        if (!string.IsNullOrEmpty(dto.LicenseNumber) &&
            await _db.Physicians.AnyAsync(p => p.LicenseNumber == dto.LicenseNumber))
        {
            return Conflict(new ApiResponse<object>
            {
                Success = false,
                Error   = $"License number '{dto.LicenseNumber}' is already registered."
            });
        }

        var physician = new Physician
        {
            PhysicianId       = Guid.NewGuid().ToString(),
            FullName          = dto.FullName,
            Specialty         = dto.Specialty,
            Email             = dto.Email,
            Phone             = dto.Phone,
            LicenseNumber     = dto.LicenseNumber,
            Credentials       = dto.Credentials,
            Institution       = dto.Institution,
            Department        = dto.Department,
            Address           = dto.Address,
            City              = dto.City,
            PreferredLanguage = dto.PreferredLanguage ?? "Arabic",
            Country           = "Yemen",
            IsActive          = true,
            CreatedAt         = DateTime.UtcNow,
            UpdatedAt         = DateTime.UtcNow
        };

        _db.Physicians.Add(physician);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Physician created — id:{Id} name:{Name}",
            physician.PhysicianId, physician.FullName);

        return CreatedAtAction(nameof(GetById),
            new { id = physician.PhysicianId },
            new ApiResponse<Physician> { Success = true, Data = physician });
    }

    // ── PUT /api/v1/Physician/{id} ────────────────────────────────────────────

    /// <summary>Updates an existing physician profile.</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<Physician>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<Physician>>> Update(
        string id, [FromBody] PhysicianCreateDto dto)
    {
        var physician = await _db.Physicians.FindAsync(id);

        if (physician == null || !physician.IsActive)
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Error   = $"Physician '{id}' not found."
            });

        physician.FullName          = dto.FullName;
        physician.Specialty         = dto.Specialty;
        physician.Email             = dto.Email;
        physician.Phone             = dto.Phone;
        physician.LicenseNumber     = dto.LicenseNumber;
        physician.Credentials       = dto.Credentials;
        physician.Institution       = dto.Institution;
        physician.Department        = dto.Department;
        physician.Address           = dto.Address;
        physician.City              = dto.City;
        physician.PreferredLanguage = dto.PreferredLanguage ?? physician.PreferredLanguage;
        physician.UpdatedAt         = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Physician updated — id:{Id}", id);

        return Ok(new ApiResponse<Physician> { Success = true, Data = physician });
    }

    // ── DELETE /api/v1/Physician/{id} ─────────────────────────────────────────

    /// <summary>Soft-deletes a physician (sets IsActive = false).</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(string id)
    {
        var physician = await _db.Physicians.FindAsync(id);

        if (physician == null || !physician.IsActive)
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Error   = $"Physician '{id}' not found."
            });

        physician.IsActive  = false;
        physician.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Physician deactivated — id:{Id}", id);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Metadata = new Dictionary<string, object> { ["physician_id"] = id }
        });
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Response DTOs (keep API surface thin — no navigation properties)
// ─────────────────────────────────────────────────────────────────────────────

public class PhysicianSummaryDto
{
    public string  PhysicianId   { get; set; } = string.Empty;
    public string  FullName      { get; set; } = string.Empty;
    public string  Specialty     { get; set; } = string.Empty;
    public string? Institution   { get; set; }
    public string? City          { get; set; }
    public int     TotalSessions { get; set; }
}

public class SessionSummaryDto
{
    public string    SessionId     { get; set; } = string.Empty;
    public string    PatientId     { get; set; } = string.Empty;
    public string    Status        { get; set; } = string.Empty;
    public string?   RiskLevel     { get; set; }
    public int?      TotalArticles { get; set; }
    public decimal?  TotalCost     { get; set; }
    public DateTime  StartedAt     { get; set; }
    public DateTime? CompletedAt   { get; set; }
}
