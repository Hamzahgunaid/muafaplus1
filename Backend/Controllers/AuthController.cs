using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
    /// Phase 3.6 Task 2: Authenticates any provider user via the unified AppUser table.
    /// Looks up the Physician record by email to populate legacy PhysicianId /
    /// Specialty / Institution claims so all existing controllers remain compatible.
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

        // Phase 3.6: look up unified AppUser by email
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

        if (user == null)
        {
            _logger.LogWarning("Login attempt for unknown email: {Email}", request.Email);
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Error   = "Invalid email or password."
            });
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login — user:{Email}", user.Email);
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Error   = "Invalid email or password."
            });
        }

        // Update last login timestamp on AppUser
        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Look up Physician record by email for backward-compat claims
        var physician = await _db.Physicians
            .FirstOrDefaultAsync(p => p.Email == request.Email && p.IsActive);

        var expiryHours = int.TryParse(_config["Jwt:ExpiryHours"], out var h) ? h : 12;
        var token       = _jwt.GenerateToken(
            user:        user,
            physicianId: physician?.PhysicianId,
            specialty:   physician?.Specialty,
            institution: physician?.Institution);

        _logger.LogInformation(
            "Successful login — user:{Email} role:{Role}", user.Email, user.Role);

        return Ok(new ApiResponse<LoginResponse>
        {
            Success = true,
            Data    = new LoginResponse
            {
                Token                = token,
                UserId               = user.UserId.ToString(),
                PhysicianId          = physician?.PhysicianId ?? string.Empty,
                FullName             = user.FullName,
                Specialty            = physician?.Specialty   ?? string.Empty,
                Institution          = physician?.Institution,
                Role                 = user.Role,
                ExpiresAt            = DateTime.UtcNow.AddHours(expiryHours),
                MustResetOnNextLogin = user.MustResetOnNextLogin
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
    /// Phase 2 Task 3: Patient login via phone number + 4-digit access code.
    /// Issues a 30-day JWT with PatientAccessId, PhoneNumber, TenantId, Role=Patient claims.
    /// Returns 401 with Arabic message if credentials are incorrect.
    /// </summary>
    [HttpPost("patient/login")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PatientLoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>),               StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PatientLoginResponse>>> PatientLogin(
        [FromBody] PatientLoginRequest request)
    {
        // Step 1: Validate credentials against PatientAccess table
        var access = await _db.PatientAccesses
            .FirstOrDefaultAsync(a => a.PhoneNumber == request.PhoneNumber
                                   && a.AccessCode  == request.Code
                                   && a.IsActive);

        if (access == null)
        {
            _logger.LogWarning("Patient login failed — phone:{Phone}", request.PhoneNumber);
            return Unauthorized(new ApiResponse<object>
            {
                Success   = false,
                Error     = "رقم الهاتف أو الرمز غير صحيح",
                ErrorType = "InvalidCredentials"
            });
        }

        // Step 2: Update last login timestamp
        access.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Step 3: Generate patient JWT (30-day expiry — patients need longer sessions)
        var jwtSecret = _config["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret not configured");

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("PatientAccessId", access.AccessId.ToString()),
            new Claim("PhoneNumber",     access.PhoneNumber),
            new Claim("TenantId",        access.TenantId.ToString()),
            new Claim("Role",            "Patient"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new JwtSecurityToken(
            issuer:             _config["Jwt:Issuer"]   ?? "muafaplus-api",
            audience:           _config["Jwt:Audience"] ?? "muafaplus-ui",
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            DateTime.UtcNow.AddDays(30),
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);

        // Step 4: Count referrals for this patient
        var referralCount = await _db.Referrals
            .CountAsync(r => r.PatientAccessId == access.AccessId);

        _logger.LogInformation(
            "Patient login success — accessId:{Id} phone:{Phone} referrals:{Count}",
            access.AccessId, access.PhoneNumber, referralCount);

        // Step 5: Return response
        return Ok(new ApiResponse<PatientLoginResponse>
        {
            Success = true,
            Data    = new PatientLoginResponse
            {
                Token         = tokenString,
                PhoneNumber   = access.PhoneNumber,
                ReferralCount = referralCount
            }
        });
    }

    /// <summary>
    /// Generates a new invitation code for the given role.
    /// Requires SuperAdmin or HospitalAdmin role.
    /// </summary>
    [HttpPost("invitation-codes/generate")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "SuperAdmin,HospitalAdmin")]
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
