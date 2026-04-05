namespace MuafaPlus.Models;

/// <summary>
/// Phase 2: High-level engagement timeline for a referral (one-to-one).
/// Tracks patient journey milestones: message received → app opened →
/// summary viewed → Stage 2 triggered → feedback submitted.
/// </summary>
public class ReferralEngagement
{
    /// <summary>PK and FK to Referral — one-to-one.</summary>
    public Guid ReferralId { get; set; }

    public DateTime? MessageSentAt       { get; set; }
    public DateTime? AppOpenedAt         { get; set; }
    public DateTime? SummaryViewedAt     { get; set; }
    public DateTime? Stage2RequestedAt   { get; set; }
    public DateTime? FeedbackSubmittedAt { get; set; }

    // Navigation properties
    public Referral? Referral { get; set; }
}
