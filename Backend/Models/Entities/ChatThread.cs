using System.ComponentModel.DataAnnotations;

namespace MuafaPlus.Models;

/// <summary>
/// Phase 3 Task 2 — Physician-Patient Async Chat.
/// One thread per referral (unique index on ReferralId).
/// ExpiresAt = referral.CreatedAt + PatientChatWindowDays (default 7).
/// MessageCount is incremented on every POST /chat/messages.
/// </summary>
public class ChatThread
{
    public Guid ThreadId { get; set; } = Guid.NewGuid();

    public Guid ReferralId { get; set; }

    [Required]
    public string PhysicianId { get; set; } = string.Empty;

    public bool     IsEnabled    { get; set; } = true;
    public DateTime ExpiresAt    { get; set; }
    public int      MessageCount { get; set; } = 0;
    public DateTime CreatedAt    { get; set; } = DateTime.UtcNow;

    // Navigation
    public Referral?                  Referral { get; set; }
    public ICollection<ChatMessage>   Messages { get; set; } = [];
}
