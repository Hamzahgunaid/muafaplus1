using System.ComponentModel.DataAnnotations;

namespace MuafaPlus.Models;

public enum TestScenarioStatus { Created, Generated, Evaluated }

/// <summary>
/// Phase 3 Task 1 — Quality System.
/// A synthetic clinical vignette used by physicians to evaluate AI output quality.
/// PatientDataJson stores a serialized PatientData object (no real patient data).
/// GeneratedContentJson stores the Stage1Output JSON after generation.
/// </summary>
public class TestScenario
{
    public Guid ScenarioId { get; set; } = Guid.NewGuid();

    [Required]
    public string PhysicianId { get; set; } = string.Empty;

    public Guid TenantId { get; set; }

    /// <summary>Full PatientData object serialized as JSON.</summary>
    [Required]
    public string PatientDataJson { get; set; } = string.Empty;

    /// <summary>Stage1Output JSON — set after generation. Null until Status = Generated.</summary>
    public string? GeneratedContentJson { get; set; }

    /// <summary>
    /// Stores generated article content keyed by
    /// article index. JSON object: { "0": "...", "1": "..." }
    /// Populated incrementally as articles are generated.
    /// </summary>
    public string? GeneratedArticlesJson { get; set; }

    public TestScenarioStatus Status { get; set; } = TestScenarioStatus.Created;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Tenant?            Tenant     { get; set; }
    public ContentEvaluation? Evaluation { get; set; }
}
