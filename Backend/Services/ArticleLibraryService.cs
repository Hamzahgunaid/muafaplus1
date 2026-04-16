using Microsoft.EntityFrameworkCore;
using MuafaPlus.Data;
using MuafaPlus.Models;

namespace MuafaPlus.Services;

/// <summary>
/// Phase 2 Task 4 — Layer 1 cost reduction.
/// Reads and writes the ArticleLibrary table for SHA-256 exact-match caching.
///
/// GetByHashAsync: returns cached Stage1ResultJson on hit, null on miss.
/// SaveAsync: persists a new library entry (race-condition-safe).
///
/// RULE 10: No TTL or expiry logic — entries are permanent.
/// </summary>
public class ArticleLibraryService
{
    private readonly MuafaDbContext              _db;
    private readonly ILogger<ArticleLibraryService> _logger;

    public ArticleLibraryService(
        MuafaDbContext               db,
        ILogger<ArticleLibraryService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Lookup
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the cached Stage1ResultJson for this profile hash, or null on miss.
    /// On hit: increments HitCount and updates LastHitAt.
    /// </summary>
    public async Task<string?> GetByHashAsync(string profileHash)
    {
        var entry = await _db.ArticleLibrary
            .FirstOrDefaultAsync(a => a.ProfileHash == profileHash);

        if (entry == null)
        {
            _logger.LogInformation(
                "LIBRARY MISS — ProfileHash:{Hash} — will generate via API",
                profileHash);
            return null;
        }

        entry.HitCount++;
        entry.LastHitAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "LIBRARY HIT — ProfileHash:{Hash} HitCount:{Count}",
            profileHash, entry.HitCount);

        return entry.Stage1ResultJson;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Save
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Saves a new library entry. If an entry for this hash already exists
    /// (race-condition guard), returns the existing entry without saving a duplicate.
    /// </summary>
    public async Task<ArticleLibrary> SaveAsync(
        string profileHash,
        string stage1ResultJson,
        Guid?  tenantId = null)
    {
        // Race-condition guard: check again inside the save path
        var existing = await _db.ArticleLibrary
            .FirstOrDefaultAsync(a => a.ProfileHash == profileHash);

        if (existing != null)
        {
            _logger.LogInformation(
                "LIBRARY SAVE skipped — entry already exists for ProfileHash:{Hash}",
                profileHash);
            return existing;
        }

        var entry = new ArticleLibrary
        {
            ProfileHash      = profileHash,
            TenantId         = tenantId,
            Stage1ResultJson = stage1ResultJson,
            HitCount         = 0,
            FirstGeneratedAt = DateTime.UtcNow,
            LastHitAt        = null
        };

        _db.ArticleLibrary.Add(entry);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "LIBRARY SAVE — ProfileHash:{Hash} LibraryId:{Id}",
            profileHash, entry.LibraryId);

        return entry;
    }
}
