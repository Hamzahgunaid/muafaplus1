using System.ComponentModel.DataAnnotations;

namespace MuafaPlus.Models;

/// <summary>
/// Phase 2: Medical profile attached to a referral (one-to-one).
/// ProfileHash (SHA-256) is populated in Phase 2 Task 4 for ArticleLibrary deduplication.
/// </summary>
public class PatientProfile
{
    public Guid ReferralId { get; set; }

    public Guid ProfileId { get; set; } = Guid.NewGuid();

    [Required]
    public string PrimaryDiagnosis { get; set; } = string.Empty;

    [Required]
    public string AgeGroup { get; set; } = string.Empty;

    public string? Comorbidities       { get; set; }
    public string? CurrentMedications  { get; set; }
    public string? Allergies           { get; set; }
    public string? MedicalRestrictions { get; set; }

    /// <summary>SHA-256 of the canonical profile fields. Populated in Phase 2 Task 4.</summary>
    [StringLength(64)]
    public string? ProfileHash { get; set; }

    /// <summary>Patient display name — applied according to tenant PatientNamePolicy.</summary>
    public string? PatientName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Referral? Referral { get; set; }
}
