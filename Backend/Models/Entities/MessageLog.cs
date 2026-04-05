using System.ComponentModel.DataAnnotations;

namespace MuafaPlus.Models;

public enum MessageType
{
    WhatsAppSummary,
    WhatsAppCode,
    SMS
}

public enum DeliveryStatus
{
    Pending,
    Sent,
    Delivered,
    Failed
}

/// <summary>
/// Phase 2: Audit log for every outbound message (WhatsApp / SMS).
/// Tracks delivery state and errors per referral.
/// </summary>
public class MessageLog
{
    public Guid MessageId { get; set; } = Guid.NewGuid();

    public Guid TenantId  { get; set; }
    public Guid ReferralId { get; set; }

    [Required]
    public string RecipientPhone { get; set; } = string.Empty;

    public MessageType    MessageType    { get; set; }
    public DeliveryStatus DeliveryStatus { get; set; } = DeliveryStatus.Pending;

    public DateTime? SentAt      { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string?   ErrorMessage { get; set; }
    public DateTime  CreatedAt   { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Tenant?   Tenant   { get; set; }
    public Referral? Referral { get; set; }
}
