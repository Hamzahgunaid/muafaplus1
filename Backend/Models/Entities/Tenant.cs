using System.ComponentModel.DataAnnotations;

namespace MuafaPlus.Models;

public class Tenant
{
    public Guid TenantId { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string NameAr { get; set; } = string.Empty;

    public string? LogoUrl { get; set; }

    public bool     IsActive  { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public TenantSettings?                   Settings               { get; set; }
    public ICollection<TenantSubscription>   Subscriptions          { get; set; } = [];
    public ICollection<InvitationCode>       InvitationCodes        { get; set; } = [];
    public ICollection<UserRole>             UserRoles              { get; set; } = [];
    public ICollection<AssistantPhysicianLink> AssistantPhysicianLinks { get; set; } = [];
}
