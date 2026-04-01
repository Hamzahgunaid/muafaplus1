using System.ComponentModel.DataAnnotations;

namespace MuafaPlus.Models;

// ─────────────────────────────────────────────────────────────────────────────
// Physician — EF Core entity
// ─────────────────────────────────────────────────────────────────────────────

public class Physician
{
    [Key]
    public string PhysicianId { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Specialty { get; set; } = string.Empty;

    [StringLength(200)]
    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(50)]
    public string? Phone { get; set; }

    [StringLength(100)]
    public string? LicenseNumber { get; set; }

    [StringLength(200)]
    public string? Credentials { get; set; }

    [StringLength(200)]
    public string? Institution { get; set; }

    [StringLength(100)]
    public string? Department { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string Country { get; set; } = "Yemen";

    [StringLength(20)]
    public string PreferredLanguage { get; set; } = "Arabic";

    public bool IsActive            { get; set; } = true;
    public bool EmailNotifications  { get; set; } = true;
    public bool SmsNotifications    { get; set; } = false;

    public DateTime  CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime  UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Patient>           Patients           { get; set; } = [];
    public ICollection<GenerationSession> GenerationSessions { get; set; } = [];
}

// ─────────────────────────────────────────────────────────────────────────────
// Patient — EF Core entity
// ─────────────────────────────────────────────────────────────────────────────

public class Patient
{
    [Key]
    public string PatientId { get; set; } = string.Empty;

    [Required]
    public string PhysicianId { get; set; } = string.Empty;

    [StringLength(300)]
    public string? PrimaryDiagnosis { get; set; }

    [StringLength(50)]
    public string? AgeGroup { get; set; }

    public string? Comorbidities      { get; set; }
    public string? CurrentMedications { get; set; }
    public string? Allergies          { get; set; }
    public string? MedicalRestrictions { get; set; }

    public DateTime  CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime  UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Physician?                     Physician          { get; set; }
    public ICollection<GenerationSession> GenerationSessions { get; set; } = [];
}

// ─────────────────────────────────────────────────────────────────────────────
// PatientData — generation request DTO
// ─────────────────────────────────────────────────────────────────────────────

public class PatientData
{
    [Required]
    public string PrimaryDiagnosis    { get; set; } = string.Empty;

    [Required]
    public string AgeGroup            { get; set; } = string.Empty;

    public string Comorbidities       { get; set; } = string.Empty;
    public string CurrentMedications  { get; set; } = string.Empty;
    public string Allergies           { get; set; } = string.Empty;
    public string MedicalRestrictions { get; set; } = string.Empty;
}

// ─────────────────────────────────────────────────────────────────────────────
// PhysicianCreateDto — create / update request DTO
// ─────────────────────────────────────────────────────────────────────────────

public class PhysicianCreateDto
{
    [Required]
    [StringLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Specialty { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Phone { get; set; }

    [StringLength(100)]
    public string? LicenseNumber { get; set; }

    [StringLength(200)]
    public string? Credentials { get; set; }

    [StringLength(200)]
    public string? Institution { get; set; }

    [StringLength(100)]
    public string? Department { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(20)]
    public string? PreferredLanguage { get; set; }
}
