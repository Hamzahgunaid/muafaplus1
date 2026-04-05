using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MuafaPlus.Data;
using MuafaPlus.Models;
using MuafaPlus.Services;

namespace MuafaPlus.Controllers;

/// <summary>
/// Authentication endpoints.
/// POST /api/v1/auth/login  — validates credentials, returns JWT
/// POST /api/v1/auth/refresh — (Phase 3: refresh token rotation)
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly MuafaDbContext _db;
    private readonly JwtService     _jwt;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _config;
    private readonly InvitationCodeService _invitationCodes;

    public AuthController(
        MuafaDbContext db,
        JwtService jwt,
        IConfiguration config,
        ILogger<AuthController> logger,
        InvitationCodeService invitationCodes)
    {
        _db              = db;
        _jwt             = jwt;
        _config          = config;
        _logger          = logger;
        _invitationCodes = invitationCodes;
    }

    /// <summary>
    /// Authenticates a physician and returns a signed JWT.
    /// The token carries the PhysicianId claim used by all downstream endpoints.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>),        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>),        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(
        [FromBody] LoginRequest request)
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

        // Look up physician by email
        var physician = await _db.Physicians
            .FirstOrDefaultAsync(p => p.Email == request.Email && p.IsActive);

        if (physician == null)
        {
            _logger.LogWarning("Login attempt for unknown email: {Email}", request.Email);
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Error   = "Invalid email or password."
            });
        }

        // Look up stored credential hash
        var credential = await _db.PhysicianCredentials
            .FindAsync(physician.PhysicianId);

        if (credential == null || !BCrypt.Net.BCrypt.Verify(request.Password, credential.PasswordHash))
        {
            _logger.LogWarning("Failed login — physician:{Id}", physician.PhysicianId);
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Error   = "Invalid email or password."
            });
        }

        // Update last login timestamp
        credential.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var expiryHours = int.TryParse(_config["Jwt:ExpiryHours"], out var h) ? h : 12;
        var token       = _jwt.GenerateToken(physician);

        _logger.LogInformation("Successful login — physician:{Id}", physician.PhysicianId);

        return Ok(new ApiResponse<LoginResponse>
        {
            Success = true,
            Data    = new LoginResponse
            {
                Token                = token,
                PhysicianId          = physician.PhysicianId,
                FullName             = physician.FullName,
                Specialty            = physician.Specialty,
                Institution          = physician.Institution,
                ExpiresAt            = DateTime.UtcNow.AddHours(expiryHours),
                MustResetOnNextLogin = credential.MustResetOnNextLogin
            }
        });
    }

    /// <summary>
    /// Changes the authenticated physician's password.
    /// Clears MustResetOnNextLogin on success.
    /// </summary>
    [HttpPost("change-password")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword(
        [FromBody] ChangePasswordRequest request)
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

        var physicianId = User.FindFirst(ClaimNames.PhysicianId)?.Value;
        if (string.IsNullOrEmpty(physicianId))
            return Unauthorized(new ApiResponse<object> { Success = false, Error = "Unauthorized." });

        var credential = await _db.PhysicianCredentials.FindAsync(physicianId);
        if (credential == null)
            return Unauthorized(new ApiResponse<object> { Success = false, Error = "Unauthorized." });

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, credential.PasswordHash))
        {
            _logger.LogWarning("Change-password: wrong current password — physician:{Id}", physicianId);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Error   = "كلمة المرور الحالية غير صحيحة."
            });
        }

        credential.PasswordHash        = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
        credential.MustResetOnNextLogin = false;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Password changed — physician:{Id}", physicianId);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Metadata = new Dictionary<string, object> { ["physician_id"] = physicianId }
        });
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Phase 1 — Invitation code endpoints
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Validates an invitation code. Always returns HTTP 200 — never 4xx —
    /// so the frontend can display IsValid=false messages gracefully.
    /// </summary>
    [HttpPost("validate-code")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [ProducesResponseType(typeof(ValidateInvitationCodeResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ValidateInvitationCodeResponse>> ValidateCode(
        [FromBody] ValidateInvitationCodeRequest request)
    {
        if (!ModelState.IsValid)
            return Ok(new ValidateInvitationCodeResponse
            {
                IsValid = false,
                Message = "Invalid request."
            });

        var result = await _invitationCodes.ValidateCodeAsync(request.Code);
        return Ok(result);
    }

    /// <summary>
    /// Patient login stub — Phase 2 will complete this when the PatientAccess
    /// table is added. Returns 501 so the Flutter app can be built against a
    /// real endpoint today.
    /// </summary>
    [HttpPost("patient/login")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PatientLoginResponse>), StatusCodes.Status501NotImplemented)]
    public ActionResult<ApiResponse<PatientLoginResponse>> PatientLogin(
        [FromBody] PatientLoginRequest request)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new ApiResponse<PatientLoginResponse>
        {
            Success   = false,
            Error     = "Patient authentication will be implemented in Phase 2 " +
                        "when PatientAccess table is added",
            ErrorType = "NotImplemented"
        });
    }

    /// <summary>
    /// Generates a new invitation code for the given role.
    /// Requires a valid physician JWT.
    /// </summary>
    [HttpPost("invitation-codes/generate")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [ProducesResponseType(typeof(ApiResponse<GenerateInvitationCodeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<GenerateInvitationCodeResponse>>> GenerateInvitationCode(
        [FromBody] GenerateInvitationCodeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ApiResponse<object>
            {
                Success = false, Error = "Validation failed.",
                Metadata = new Dictionary<string, object>
                {
                    ["errors"] = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                }
            });

        var userId = User.FindFirst(ClaimNames.PhysicianId)?.Value ?? string.Empty;

        try
        {
            var result = await _invitationCodes.GenerateCodeAsync(request, userId);
            _logger.LogInformation(
                "Invitation code generated — {Code} by physician:{UserId}", result.Code, userId);
            return Ok(new ApiResponse<GenerateInvitationCodeResponse> { Success = true, Data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate invitation code");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false, Error = "Failed to generate invitation code.", ErrorType = ex.GetType().Name
            });
        }
    }

    /// <summary>
    /// Returns the profile of the currently authenticated physician.
    /// Useful for the frontend to bootstrap the session from a stored token.
    /// </summary>
    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [ProducesResponseType(typeof(ApiResponse<PhysicianSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PhysicianSummaryDto>>> Me()
    {
        var physicianId = User.FindFirst(ClaimNames.PhysicianId)?.Value;
        if (string.IsNullOrEmpty(physicianId))
            return Unauthorized();

        var physician = await _db.Physicians.FindAsync(physicianId);
        if (physician == null || !physician.IsActive)
            return Unauthorized();

        return Ok(new ApiResponse<PhysicianSummaryDto>
        {
            Success = true,
            Data    = new PhysicianSummaryDto
            {
                PhysicianId  = physician.PhysicianId,
                FullName     = physician.FullName,
                Specialty    = physician.Specialty,
                Institution  = physician.Institution,
                City         = physician.City,
                TotalSessions = await _db.GenerationSessions
                    .CountAsync(s => s.PhysicianId == physicianId)
            }
        });
    }
}
