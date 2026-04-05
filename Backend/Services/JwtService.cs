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

    public SymmetricSecurityKey GetSigningKey()
        => new(Encoding.UTF8.GetBytes(Secret));
}
