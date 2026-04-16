using System.ComponentModel.DataAnnotations;

namespace MuafaPlus.Models;

/// <summary>
/// Phase 2: Stores patient phone + 4-digit access code for login.
/// One record per patient per tenant. Phone stored as plain text —
/// hashing deferred to Phase 3 security hardening.
/// </summary>
public class PatientAccess
{
    public Guid TenantId { get; set; }

    public Guid AccessId { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>4-digit numeric code, stored as string e.g. "4829".</summary>
    [Required]
    [StringLength(4)]
    public string AccessCode { get; set; } = string.Empty;

    public bool      IsActive    { get; set; } = true;
    public DateTime  CreatedAt   { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public Tenant?              Tenant   { get; set; }
    public ICollection<Referral> Referrals { get; set; } = [];
}
