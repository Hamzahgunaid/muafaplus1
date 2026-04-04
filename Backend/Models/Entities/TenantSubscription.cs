using System.ComponentModel.DataAnnotations;

namespace MuafaPlus.Models;

public class TenantSubscription
{
    public Guid TenantId       { get; set; }
    public Guid SubscriptionId { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(50)]
    public string PlanType { get; set; } = string.Empty;

    public int      CasesAllocated   { get; set; }
    public int      CasesUsed        { get; set; } = 0;
    public DateTime BillingCycleStart { get; set; }
    public DateTime BillingCycleEnd   { get; set; }
    public bool     IsActive          { get; set; } = true;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
}
