using System.ComponentModel.DataAnnotations;

namespace MuafaPlus.Models;

/// <summary>
/// Phase 3.6 Task 2: Unified user table replacing the separate
/// Physician + PhysicianCredentials authentication flow.
/// All provider user types (SuperAdmin, HospitalAdmin, Physician, Assistant)
/// authenticate through this table.
/// </summary>
public class AppUser
{
    [Key]
    public Guid UserId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Null for SuperAdmin (global scope).
    /// Non-null for all tenant-scoped users.
    /// </summary>
    public Guid? TenantId { get; set; }

    [Required]
    [StringLength(200)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// System role written to the JWT "Role" claim.
    /// Accepted values: SuperAdmin | HospitalAdmin | Physician | Assistant
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Role { get; set; } = "Physician";

    [StringLength(20)]
    public string? Mobile { get; set; }

    public bool      IsActive             { get; set; } = true;
    public bool      MustResetOnNextLogin { get; set; } = false;
    public DateTime  CreatedAt            { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt          { get; set; }
}
