namespace MuafaPlus.Models;

public class UserRole
{
    /// <summary>
    /// Phase 3.6: changed from string to Guid to match AppUser.UserId (uuid in PostgreSQL).
    /// </summary>
    public Guid       UserId     { get; set; }
    public Guid       TenantId   { get; set; }
    public TenantRole Role       { get; set; }
    public DateTime   AssignedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public AppUser User   { get; set; } = null!;
    public Tenant  Tenant { get; set; } = null!;
}
