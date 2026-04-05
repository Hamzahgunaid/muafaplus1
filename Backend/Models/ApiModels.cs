using System.ComponentModel.DataAnnotations;

namespace MuafaPlus.Models;

// ─────────────────────────────────────────────────────────────────────────────
// Database entity
// ─────────────────────────────────────────────────────────────────────────────

public class GenerationSession
{
    [Key]
    public string SessionId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string PatientId { get; set; } = string.Empty;

    [Required]
    public string PhysicianId { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Stage { get; set; } = "stage_1";

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "pending";

    [StringLength(20)]
    public string? RiskLevel { get; set; }

    public int?     TotalArticles { get; set; }
    public decimal? TotalCost     { get; set; }

    public DateTime  StartedAt   { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    [StringLength(500)]
    public string? ErrorMessage { get; set; }

    // Navigation properties
    public Patient?   Patient   { get; set; }
    public Physician? Physician { get; set; }
    public ICollection<GeneratedArticle> GeneratedArticles { get; set; } = [];
}

// ─────────────────────────────────────────────────────────────────────────────
// API wrapper types
// ─────────────────────────────────────────────────────────────────────────────

public class ApiResponse<T>
{
    public bool                        Success   { get; set; }
    public T?                          Data      { get; set; }
    public string?                     Error     { get; set; }
    public string?                     ErrorType { get; set; }
    public Dictionary<string, object>? Metadata  { get; set; }
}

public class TokenUsage
{
    public int InputTokens         { get; set; }
    public int OutputTokens        { get; set; }
    public int CacheCreationTokens { get; set; }
    public int CacheReadTokens     { get; set; }

    /// <summary>
    /// Cost calculation using Sonnet 4 pricing (USD per million tokens).
    /// Input: $3 | Output: $15 | Cache write: $3.75 | Cache read: $0.30
    /// </summary>
    public decimal CalculateCost()
    {
        const decimal INPUT_COST       = 3.00m   / 1_000_000;
        const decimal OUTPUT_COST      = 15.00m  / 1_000_000;
        const decimal CACHE_WRITE_COST = 3.75m   / 1_000_000;
        const decimal CACHE_READ_COST  = 0.30m   / 1_000_000;

        return (InputTokens         * INPUT_COST)       +
               (OutputTokens        * OUTPUT_COST)      +
               (CacheCreationTokens * CACHE_WRITE_COST) +
               (CacheReadTokens     * CACHE_READ_COST);
    }
}

public class Stage1Result
{
    public bool         Success      { get; set; }
    public Stage1Output? Output      { get; set; }
    public TokenUsage   TokenUsage   { get; set; } = new();
    public string       Model        { get; set; } = string.Empty;
    public string?      ErrorMessage { get; set; }
    public string?      ErrorType    { get; set; }
}

public class Stage2Result
{
    public bool          Success      { get; set; }
    public Stage2Output? Output       { get; set; }
    public TokenUsage    TokenUsage   { get; set; } = new();
    public string        Model        { get; set; } = string.Empty;
    public string?       ErrorMessage { get; set; }
    public string?       ErrorType    { get; set; }
}

public class WorkflowResult
{
    public string SessionId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public bool   Success   { get; set; }

    /// <summary>
    /// The deterministic risk score computed by RiskCalculatorService in C#.
    /// This is the authoritative value; RiskAssessment below is what the AI echoed back.
    /// </summary>
    public RiskScore?      CSharpRiskScore   { get; set; }
    public string?         SummaryArticle    { get; set; }
    public RiskAssessment? RiskAssessment    { get; set; }

    public List<ArticleContent> DetailedArticles { get; set; } = [];

    public decimal Stage1Cost { get; set; }
    public decimal Stage2Cost { get; set; }
    public decimal TotalCost  { get; set; }

    public TokenUsage       Stage1Tokens { get; set; } = new();
    public List<TokenUsage> Stage2Tokens { get; set; } = [];

    public string? ErrorMessage { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// Request DTOs
// ─────────────────────────────────────────────────────────────────────────────

public class GenerateContentRequest
{
    // PhysicianId is extracted from the JWT claim in the controller (Rule 4).
    // Accepted in the body as a fallback for tooling/tests only — not required.
    public string? PhysicianId { get; set; }

    [Required]
    public PatientData PatientData { get; set; } = new();

    public bool GenerateAllArticles { get; set; } = true;
}

// ─────────────────────────────────────────────────────────────────────────────
// Phase 1 — Invitation code models
// ─────────────────────────────────────────────────────────────────────────────

public class ValidateInvitationCodeRequest
{
    [Required]
    public string Code { get; set; } = string.Empty;
}

public class ValidateInvitationCodeResponse
{
    public bool    IsValid    { get; set; }
    public string  Role       { get; set; } = string.Empty;
    public Guid?   TenantId   { get; set; }
    public string? TenantName { get; set; }
    public string  Message    { get; set; } = string.Empty;
}

public class PatientLoginRequest
{
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;  // format: +967XXXXXXXXX

    [Required]
    public string Code { get; set; } = string.Empty;         // 4-digit referral code
}

public class PatientLoginResponse
{
    public string Token         { get; set; } = string.Empty;
    public string PhoneNumber   { get; set; } = string.Empty;
    public int    ReferralCount { get; set; }
}

public class GenerateInvitationCodeRequest
{
    public Guid?      TenantId     { get; set; }         // null for SuperAdmin codes
    public TenantRole Role         { get; set; }
    public int        ExpiresInDays { get; set; } = 30;
}

public class GenerateInvitationCodeResponse
{
    public string    Code      { get; set; } = string.Empty;
    public string    Role      { get; set; } = string.Empty;
    public DateTime  ExpiresAt { get; set; }
    public Guid?     TenantId  { get; set; }
}
