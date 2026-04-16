using System.ComponentModel.DataAnnotations;

namespace MuafaPlus.Models;

public class InvitationCode
{
    [Key]
    [StringLength(10)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Null for SuperAdmin invitation codes (SA-XXXXXX), which are not
    /// scoped to any tenant.
    /// </summary>
    public Guid? TenantId { get; set; }

    public TenantRole Role            { get; set; }
    public string?    CreatedByUserId { get; set; }
    public string?    UsedByUserId    { get; set; }
    public DateTime?  UsedAt          { get; set; }
    public DateTime?  ExpiresAt       { get; set; }
    public bool       IsActive        { get; set; } = true;
    public DateTime   CreatedAt       { get; set; } = DateTime.UtcNow;

    // Navigation
    public Tenant? Tenant { get; set; }
}
