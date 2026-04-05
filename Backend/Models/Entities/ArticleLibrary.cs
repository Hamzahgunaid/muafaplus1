using System.ComponentModel.DataAnnotations;

namespace MuafaPlus.Models;

/// <summary>
/// Phase 2 Task 4 — Layer 1 cost reduction.
/// Stores the full Stage 1 JSON output keyed by a SHA-256 profile hash.
/// Before every Claude API call, WorkflowService checks this table.
/// After every generation, the result is persisted here.
///
/// TenantId = null means the entry is shared across all tenants.
///
/// RULE 10: This table has no TTL or expiry — entries are permanent.
/// Phase 5 will add a vector (1024-dim) column for pgvector near-match lookup.
/// </summary>
public class ArticleLibrary
{
    public Guid LibraryId { get; set; } = Guid.NewGuid();

    /// <summary>SHA-256 hex string of the canonical patient profile fields.</summary>
    [Required]
    [StringLength(64)]
    public string ProfileHash { get; set; } = string.Empty;

    /// <summary>Null = shared across all tenants (RULE 10).</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Full Stage1Output serialised as JSON — deserialized on cache hit.</summary>
    [Required]
    public string Stage1ResultJson { get; set; } = string.Empty;

    /// <summary>How many times this entry has been served from cache.</summary>
    public int HitCount { get; set; } = 0;

    public DateTime  FirstGeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastHitAt        { get; set; }
}
