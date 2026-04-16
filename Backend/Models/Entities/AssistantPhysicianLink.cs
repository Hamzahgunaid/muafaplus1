namespace MuafaPlus.Models;

public class AssistantPhysicianLink
{
    public Guid LinkId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Both AssistantId and PhysicianId reference physician/user identifiers.
    /// No database-level FK constraints — see UserRole.UserId comment.
    /// </summary>
    public string AssistantId { get; set; } = string.Empty;
    public string PhysicianId { get; set; } = string.Empty;

    public Guid     TenantId { get; set; }
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
    public bool     IsActive { get; set; } = true;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
}
