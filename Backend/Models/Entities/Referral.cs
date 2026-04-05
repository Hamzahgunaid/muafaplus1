using System.ComponentModel.DataAnnotations;

namespace MuafaPlus.Models;

public enum ReferralStatus
{
    Created,
    Stage1Complete,
    Stage1Delivered,
    Stage2Requested,
    Stage2Complete,
    FeedbackSubmitted
}

/// <summary>
/// Phase 2: Core referral record. Links a patient (via PatientAccess)
/// to a generation session. Tracks delivery and status through the
/// full patient workflow.
/// </summary>
public class Referral
{
    public Guid ReferralId { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    [Required]
    public string PhysicianId { get; set; } = string.Empty;

    /// <summary>UserId of the person who created this referral (Physician or Assistant).</summary>
    [Required]
    public string CreatedByUserId { get; set; } = string.Empty;

    public Guid PatientAccessId { get; set; }

    /// <summary>Links to GenerationSessions.SessionId. Populated after Stage 1 completes.</summary>
    public string? SessionId { get; set; }

    public ReferralStatus Status { get; set; } = ReferralStatus.Created;

    /// <summary>Low / Moderate / High / Critical — from RiskCalculatorService.</summary>
    [StringLength(20)]
    public string? RiskLevel { get; set; }

    public bool      WhatsAppDelivery    { get; set; } = true;
    public DateTime? ScheduledDeliveryAt { get; set; }
    public DateTime? DeliveredAt         { get; set; }
    public DateTime  CreatedAt           { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Tenant?              Tenant         { get; set; }
    public PatientAccess?       PatientAccess  { get; set; }
    public PatientProfile?      Profile        { get; set; }
    public ReferralEngagement?  Engagement     { get; set; }
    public PatientFeedback?     Feedback       { get; set; }
    public ICollection<ArticleEngagement> ArticleEngagements { get; set; } = [];
    public ICollection<MessageLog>        MessageLogs        { get; set; } = [];
}
