using Microsoft.EntityFrameworkCore;
using MuafaPlus.Data;
using MuafaPlus.Models;

namespace MuafaPlus.Services;

/// <summary>
/// Phase 1: validates, generates, and marks invitation codes.
/// Code formats:
///   SA-XXXXXX → SuperAdmin  (TenantId = null)
///   HA-XXXXXX → HospitalAdmin
///   PH-XXXXXX → Physician
///   AS-XXXXXX → Assistant
/// </summary>
public class InvitationCodeService
{
    private readonly MuafaDbContext _db;
    private readonly ILogger<InvitationCodeService> _logger;

    private static readonly char[] Chars =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

    public InvitationCodeService(MuafaDbContext db, ILogger<InvitationCodeService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Validate
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<ValidateInvitationCodeResponse> ValidateCodeAsync(string code)
    {
        var entry = await _db.InvitationCodes
            .Include(c => c.Tenant)
            .FirstOrDefaultAsync(c => c.Code == code);

        if (entry == null)
        {
            _logger.LogDebug("Validate code: not found — {Code}", code);
            return Invalid("Invalid code");
        }

        if (!entry.IsActive)
        {
            _logger.LogDebug("Validate code: already used — {Code}", code);
            return Invalid("Code already used");
        }

        if (entry.ExpiresAt.HasValue && entry.ExpiresAt.Value < DateTime.UtcNow)
        {
            _logger.LogDebug("Validate code: expired — {Code}", code);
            return Invalid("Code expired");
        }

        _logger.LogInformation("Validate code: valid — {Code} role:{Role}", code, entry.Role);

        return new ValidateInvitationCodeResponse
        {
            IsValid    = true,
            Role       = entry.Role.ToString(),
            TenantId   = entry.TenantId,
            TenantName = entry.Tenant?.Name,
            Message    = "Code is valid"
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Generate
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<GenerateInvitationCodeResponse> GenerateCodeAsync(
        GenerateInvitationCodeRequest request,
        string createdByUserId)
    {
        var prefix = request.Role switch
        {
            TenantRole.SuperAdmin    => "SA-",
            TenantRole.HospitalAdmin => "HA-",
            TenantRole.Physician     => "PH-",
            TenantRole.Assistant     => "AS-",
            _ => throw new ArgumentOutOfRangeException(nameof(request.Role), request.Role, null)
        };

        string code = string.Empty;
        const int maxAttempts = 5;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var candidate = prefix + RandomSuffix();
            var exists = await _db.InvitationCodes.AnyAsync(c => c.Code == candidate);
            if (!exists)
            {
                code = candidate;
                break;
            }
            _logger.LogDebug("Code collision on attempt {N}: {Code}", attempt + 1, candidate);
        }

        if (string.IsNullOrEmpty(code))
            throw new InvalidOperationException(
                "Failed to generate a unique invitation code after 5 attempts.");

        var expiresAt = DateTime.UtcNow.AddDays(request.ExpiresInDays);

        var entry = new InvitationCode
        {
            Code            = code,
            TenantId        = request.TenantId,
            Role            = request.Role,
            CreatedByUserId = createdByUserId,
            IsActive        = true,
            ExpiresAt       = expiresAt,
            CreatedAt       = DateTime.UtcNow
        };

        _db.InvitationCodes.Add(entry);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Generated invitation code — {Code} role:{Role} expiresAt:{ExpiresAt}",
            code, request.Role, expiresAt);

        return new GenerateInvitationCodeResponse
        {
            Code      = code,
            Role      = request.Role.ToString(),
            ExpiresAt = expiresAt,
            TenantId  = request.TenantId
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Mark used
    // ─────────────────────────────────────────────────────────────────────────

    public async Task MarkCodeUsedAsync(string code, string usedByUserId)
    {
        var entry = await _db.InvitationCodes.FindAsync(code);
        if (entry == null) return;

        entry.IsActive      = false;
        entry.UsedByUserId  = usedByUserId;
        entry.UsedAt        = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Invitation code marked used — {Code} usedBy:{UserId}", code, usedByUserId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static ValidateInvitationCodeResponse Invalid(string message) =>
        new() { IsValid = false, Message = message };

    private static string RandomSuffix() =>
        string.Create(6, Random.Shared, static (span, rng) =>
        {
            for (int i = 0; i < span.Length; i++)
                span[i] = Chars[rng.Next(Chars.Length)];
        });
}
