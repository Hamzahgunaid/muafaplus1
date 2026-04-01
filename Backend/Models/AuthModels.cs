using System.ComponentModel.DataAnnotations;

namespace MuafaPlus.Models;

// ── Login request ─────────────────────────────────────────────────────────────

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
}

// ── Login response ────────────────────────────────────────────────────────────

public class LoginResponse
{
    public string Token        { get; set; } = string.Empty;
    public string PhysicianId  { get; set; } = string.Empty;
    public string FullName     { get; set; } = string.Empty;
    public string Specialty    { get; set; } = string.Empty;
    public string? Institution { get; set; }
    public DateTime ExpiresAt  { get; set; }
}

// ── Physician credentials entity ──────────────────────────────────────────────
// Stored separately from the Physician profile to keep authentication
// concerns isolated. Uses bcrypt hashing — never store plain passwords.

public class PhysicianCredential
{
    [Key]
    public string PhysicianId    { get; set; } = string.Empty;

    [Required]
    public string PasswordHash   { get; set; } = string.Empty;

    public bool   MustResetOnNextLogin { get; set; } = true;
    public DateTime CreatedAt    { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    // Navigation
    public Physician? Physician  { get; set; }
}

// ── Token claims helper ───────────────────────────────────────────────────────

public static class ClaimNames
{
    public const string PhysicianId = System.Security.Claims.ClaimTypes.NameIdentifier;
    public const string FullName    = System.Security.Claims.ClaimTypes.Name;
    public const string Email       = System.Security.Claims.ClaimTypes.Email;
    public const string Specialty   = "specialty";
    public const string Institution = "institution";
}
