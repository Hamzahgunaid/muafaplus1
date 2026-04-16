using System.ComponentModel.DataAnnotations;

namespace MuafaPlus.Models;

/// <summary>
/// Phase 2: Patient feedback submitted after reading their referral articles.
/// One feedback record per referral (one-to-one).
/// </summary>
public class PatientFeedback
{
    public Guid FeedbackId { get; set; } = Guid.NewGuid();

    public Guid ReferralId { get; set; }

    public bool IsHelpful { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Referral? Referral { get; set; }
}
