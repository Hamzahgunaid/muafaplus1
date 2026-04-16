using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MuafaPlus.Models;

namespace MuafaPlus.Services;

/// <summary>
/// Issues and validates JWT tokens scoped to a PhysicianId.
/// Configure via appsettings: Jwt:Secret, Jwt:Issuer, Jwt:Audience, Jwt:ExpiryHours
/// </summary>
public class JwtService
{
    private readonly IConfiguration _config;
    private readonly ILogger<JwtService> _logger;

    private string Secret   => _config["Jwt:Secret"]   ?? throw new InvalidOperationException("Jwt:Secret not configured");
    private string Issuer   => _config["Jwt:Issuer"]   ?? "muafaplus-api";
    private string Audience => _config["Jwt:Audience"] ?? "muafaplus-ui";
    private int    Expiry   => int.TryParse(_config["Jwt:ExpiryHours"], out var h) ? h : 12;

    public JwtService(IConfiguration config, ILogger<JwtService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public string GenerateToken(Physician physician)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, physician.PhysicianId),
            new Claim(ClaimTypes.Name,            physician.FullName),
            new Claim(ClaimTypes.Email,           physician.Email ?? string.Empty),
            new Claim("specialty",                physician.Specialty),
            new Claim("institution",              physician.Institution ?? string.Empty),
            new Claim("TenantId",                 physician.TenantId?.ToString() ?? ""),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer:             Issuer,
            audience:           Audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            DateTime.UtcNow.AddHours(Expiry),
            signingCredentials: creds
        );

        _logger.LogInformation("JWT issued — physician:{Id} expiry:{Expiry}h",
            physician.PhysicianId, Expiry);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Phase 3.6 Task 2: issues a JWT for an AppUser.
    /// Sets ClaimTypes.NameIdentifier to <paramref name="physicianId"/> when provided
    /// so all existing controllers that read ClaimNames.PhysicianId continue to work.
    /// The "Role" claim enables [Authorize(Roles = "...")] enforcement.
    /// </summary>
    public string GenerateToken(
        AppUser user,
        string? physicianId  = null,
        string? specialty    = null,
        string? institution  = null)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            // Keep NameIdentifier = PhysicianId for backward compat with all controllers.
            // Falls back to UserId string for non-physician accounts (e.g. SuperAdmin).
            new Claim(ClaimTypes.NameIdentifier, physicianId ?? user.UserId.ToString()),
            new Claim(ClaimTypes.Name,           user.FullName),
            new Claim(ClaimTypes.Email,          user.Email),
            new Claim(ClaimNames.UserId,         user.UserId.ToString()),
            new Claim(ClaimNames.Role,           user.Role),
            new Claim(ClaimNames.TenantId,       user.TenantId?.ToString() ?? ""),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (!string.IsNullOrEmpty(specialty))
            claims.Add(new Claim("specialty",    specialty));
        if (!string.IsNullOrEmpty(institution))
            claims.Add(new Claim("institution",  institution));

        var token = new JwtSecurityToken(
            issuer:             Issuer,
            audience:           Audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            DateTime.UtcNow.AddHours(Expiry),
            signingCredentials: creds
        );

        _logger.LogInformation(
            "JWT issued — user:{Email} role:{Role} expiry:{Expiry}h",
            user.Email, user.Role, Expiry);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public SymmetricSecurityKey GetSigningKey()
        => new(Encoding.UTF8.GetBytes(Secret));
}
