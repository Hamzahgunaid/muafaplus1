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

    public AuthController(
        MuafaDbContext db,
        JwtService jwt,
        IConfiguration config,
        ILogger<AuthController> logger)
    {
        _db     = db;
        _jwt    = jwt;
        _config = config;
        _logger = logger;
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
