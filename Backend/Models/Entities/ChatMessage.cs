using System.ComponentModel.DataAnnotations;

namespace MuafaPlus.Models;

public enum SenderRole { Physician, Patient }

/// <summary>
/// Phase 3 Task 2 — Physician-Patient Async Chat.
/// Belongs to a ChatThread. SenderRole stored as string (Npgsql convention).
/// IsRead is set to true when the other party next sends a message.
/// </summary>
public class ChatMessage
{
    public Guid MessageId { get; set; } = Guid.NewGuid();

    public Guid ThreadId { get; set; }

    public SenderRole SenderRole { get; set; }

    [Required]
    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;

    public DateTime SentAt  { get; set; } = DateTime.UtcNow;
    public bool     IsRead  { get; set; } = false;

    // Navigation
    public ChatThread? Thread { get; set; }
}
