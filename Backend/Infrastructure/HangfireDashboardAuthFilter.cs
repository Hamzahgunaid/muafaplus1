using System.Text;
using Hangfire.Dashboard;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace MuafaPlus.Infrastructure;

/// <summary>
/// Protects the Hangfire dashboard at /hangfire by requiring a valid JWT
/// Bearer token in the Authorization header. Uses the same Jwt:Secret,
/// Jwt:Issuer, and Jwt:Audience values as the main authentication middleware.
/// Returns false (→ HTTP 401) for missing or invalid tokens.
/// </summary>
public class HangfireDashboardAuthFilter : IDashboardAuthorizationFilter
{
    private readonly TokenValidationParameters _validationParams;

    public HangfireDashboardAuthFilter(IConfiguration config)
    {
        var secret = config["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret must be configured.");

        _validationParams = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = config["Jwt:Issuer"]   ?? "muafaplus-api",
            ValidAudience            = config["Jwt:Audience"] ?? "muafaplus-ui",
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ClockSkew                = TimeSpan.FromMinutes(1)
        };
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return false;

        var token = authHeader["Bearer ".Length..].Trim();
        if (string.IsNullOrEmpty(token))
            return false;

        try
        {
            new JwtSecurityTokenHandler().ValidateToken(token, _validationParams, out _);
            return true;
        }
        catch (SecurityTokenException)
        {
            return false;
        }
    }
}
