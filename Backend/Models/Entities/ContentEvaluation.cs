using System.ComponentModel.DataAnnotations;

namespace MuafaPlus.Models;

/// <summary>
/// Phase 3 Task 1 — Quality System.
/// Physician evaluation of AI-generated content for a TestScenario.
/// One evaluation per scenario (unique index on ScenarioId).
/// </summary>
public class ContentEvaluation
{
    public Guid EvaluationId { get; set; } = Guid.NewGuid();

    public Guid ScenarioId { get; set; }

    [Required]
    public string PhysicianId { get; set; } = string.Empty;

    [Range(1, 5)]
    public int AccuracyRating { get; set; }

    [Range(1, 5)]
    public int ClarityRating { get; set; }

    [Range(1, 5)]
    public int RelevanceRating { get; set; }

    [Range(1, 5)]
    public int CompletenessRating { get; set; }

    public bool IsAppropriate        { get; set; }
    public bool IsCulturallySensitive { get; set; }
    public bool IsArabicQuality      { get; set; }

    [StringLength(2000)]
    public string? WhatWorked { get; set; }

    [StringLength(2000)]
    public string? NeedsImprovement { get; set; }

    [StringLength(2000)]
    public string? Comments { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public TestScenario? Scenario { get; set; }
}
