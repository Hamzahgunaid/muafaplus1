using System.ComponentModel.DataAnnotations;

namespace MuafaPlus.Models;

public enum ArticleReaction
{
    None,
    Like,
    Dislike
}

/// <summary>
/// Phase 2: Per-article engagement tracking for a referral.
/// Records scroll depth milestones and patient reaction (Like/Dislike).
/// </summary>
public class ArticleEngagement
{
    public Guid EngagementId { get; set; } = Guid.NewGuid();

    public Guid ReferralId { get; set; }

    /// <summary>Matches GeneratedArticles.ArticleId.</summary>
    [Required]
    public string ArticleId { get; set; } = string.Empty;

    public DateTime? OpenedAt    { get; set; }
    public DateTime? Depth25At   { get; set; }
    public DateTime? Depth50At   { get; set; }
    public DateTime? Depth75At   { get; set; }
    public DateTime? CompletedAt { get; set; }

    public int             TimeOnArticleSeconds { get; set; } = 0;
    public ArticleReaction Reaction             { get; set; } = ArticleReaction.None;

    // Navigation properties
    public Referral? Referral { get; set; }
}
