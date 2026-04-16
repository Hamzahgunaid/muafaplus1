using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MuafaPlus.Models;

/// <summary>Article specification produced by Stage 1 outline generation.</summary>
public class ArticleSpec
{
    [Required]
    [RegularExpression(@"^[a-z_]+_\d{3}$")]
    [JsonPropertyName("article_id")]
    public string ArticleId { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("title_ar")]
    public string TitleAr { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("title_en")]
    public string TitleEn { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    [MaxLength(2)]
    [JsonPropertyName("coverage_codes")]
    public List<string> CoverageCodes { get; set; } = [];

    [Required]
    [RegularExpression("^(CRITICAL|HIGH|MEDIUM|REFERENCE)$")]
    [JsonPropertyName("priority")]
    public string Priority { get; set; } = "MEDIUM";

    [Required]
    [JsonPropertyName("estimated_word_count")]
    public string EstimatedWordCount { get; set; } = string.Empty;

    [Required]
    [MaxLength(3)]
    [JsonPropertyName("key_topics")]
    public List<string> KeyTopics { get; set; } = [];

    [Required]
    [MinLength(10)]
    [JsonPropertyName("rationale")]
    public string Rationale { get; set; } = string.Empty;
}

/// <summary>
/// Risk assessment data returned inside the Stage 1 JSON response.
/// Note: the authoritative risk calculation is performed by RiskCalculatorService
/// in C# before the AI call. This model captures what the AI echoes back for
/// audit trail purposes.
/// </summary>
public class RiskAssessment
{
    [JsonPropertyName("acute_factors")]
    public List<string> AcuteFactors      { get; set; } = [];

    [JsonPropertyName("acute_points")]
    public decimal      AcutePoints       { get; set; }

    [JsonPropertyName("complexity_factors")]
    public List<string> ComplexityFactors { get; set; } = [];

    [JsonPropertyName("complexity_points")]
    public decimal      ComplexityPoints  { get; set; }

    [JsonPropertyName("protective_factors")]
    public List<string> ProtectiveFactors { get; set; } = [];

    [JsonPropertyName("protective_points")]
    public decimal      ProtectivePoints  { get; set; }

    [JsonPropertyName("total_score")]
    public decimal      TotalScore        { get; set; }

    [RegularExpression("^(LOW|MODERATE|HIGH|CRITICAL)$")]
    [JsonPropertyName("risk_level")]
    public string       RiskLevel         { get; set; } = "MODERATE";

    [JsonPropertyName("rationale")]
    public string       Rationale         { get; set; } = string.Empty;
}

/// <summary>Stage 1 full output from Claude API — deserialized from JSON response.</summary>
public class Stage1Output
{
    [JsonPropertyName("risk_assessment")]
    public RiskAssessment RiskAssessment { get; set; } = new();

    [JsonPropertyName("summary_article")]
    public string SummaryArticle { get; set; } = string.Empty;

    [JsonPropertyName("article_outlines")]
    public List<ArticleSpec> ArticleOutlines { get; set; } = [];

    [JsonPropertyName("metadata")]
    public Stage1Metadata Metadata { get; set; } = new();
}

public class Stage1Metadata
{
    [JsonPropertyName("total_articles")]
    public int TotalArticles { get; set; }

    [JsonPropertyName("generation_timestamp")]
    public DateTime GenerationTimestamp { get; set; }

    [JsonPropertyName("ramadan_period")]
    public bool RamadanPeriod { get; set; }
}

public class SourceCitation
{
    public string Title { get; set; } = string.Empty;
    public string Url   { get; set; } = string.Empty;
    public int    Year  { get; set; }
}

/// <summary>
/// Stage 2 full output from Claude API.
/// The "article" wrapper matches the updated prompt schema.
/// </summary>
public class Stage2Output
{
    [JsonPropertyName("article")]
    public ArticleContent Article { get; set; } = new();

    [JsonPropertyName("metadata")]
    public Stage2Metadata Metadata { get; set; } = new();
}

/// <summary>
/// Generated article content.
/// FIXED: ContentAr uses JsonPropertyName "content_ar" — matches Stage 2 prompt schema.
/// Previously the model used "content" which did not match and caused silent mapping failures.
/// </summary>
public class ArticleContent
{
    [JsonPropertyName("article_id")]
    public string ArticleId { get; set; } = string.Empty;

    [JsonPropertyName("title_ar")]
    public string TitleAr { get; set; } = string.Empty;

    [JsonPropertyName("title_en")]
    public string TitleEn { get; set; } = string.Empty;

    [JsonPropertyName("coverage_codes")]
    public List<string> CoverageCodes { get; set; } = [];

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = string.Empty;

    [JsonPropertyName("risk_level")]
    public string RiskLevel { get; set; } = string.Empty;

    /// <summary>
    /// Full Arabic article markdown.
    /// JSON key is "content_ar" — matches the prompt schema.
    /// </summary>
    [JsonPropertyName("content_ar")]
    public string ContentAr { get; set; } = string.Empty;

    [JsonPropertyName("word_count")]
    public int WordCount { get; set; }

    [JsonPropertyName("sections_included")]
    public List<int> SectionsIncluded { get; set; } = [];

    [JsonPropertyName("sources")]
    public List<SourceCitation> Sources { get; set; } = [];
}

public class Stage2Metadata
{
    [JsonPropertyName("generation_timestamp")]
    public DateTime GenerationTimestamp { get; set; }

    [JsonPropertyName("risk_level")]
    public string RiskLevel { get; set; } = string.Empty;

    [JsonPropertyName("patient_age_group")]
    public string PatientAgeGroup { get; set; } = string.Empty;

    [JsonPropertyName("ramadan_period")]
    public bool RamadanPeriod { get; set; }

    [JsonPropertyName("yemen_context_applied")]
    public bool YemenContextApplied { get; set; }
}

/// <summary>Database entity for a generated article record.</summary>
public class GeneratedArticle
{
    [Key]
    public string ArticleId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string ArticleType { get; set; } = "detailed"; // "summary" | "detailed"

    [StringLength(200)]
    public string? CoverageCodes { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public int     WordCount    { get; set; }
    public int     TokensInput  { get; set; }
    public int     TokensOutput { get; set; }
    public decimal CostUsd      { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public GenerationSession? GenerationSession { get; set; }
}
