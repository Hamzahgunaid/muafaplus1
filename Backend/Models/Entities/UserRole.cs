namespace MuafaPlus.Models;

public class UserRole
{
    /// <summary>
    /// References the physician/user identifier. No database-level FK constraint
    /// here because Phase 2 will introduce a unified Users table that spans all
    /// user types. The index still enforces fast lookups.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    public Guid       TenantId   { get; set; }
    public TenantRole Role       { get; set; }
    public DateTime   AssignedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
}
