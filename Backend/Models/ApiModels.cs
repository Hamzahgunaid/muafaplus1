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

// ─────────────────────────────────────────────────────────────────────────────
// Phase 1 — Tenant management models
// ─────────────────────────────────────────────────────────────────────────────

public class CreateTenantRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string NameAr { get; set; } = string.Empty;

    public string? LogoUrl { get; set; }

    [Required]
    [EmailAddress]
    public string AdminEmail { get; set; } = string.Empty;

    [Required]
    public string PlanType { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "CasesAllocated must be at least 1.")]
    public int CasesAllocated { get; set; }
}

public class TenantSettingsResponse
{
    public string  PatientNamePolicy      { get; set; } = string.Empty;
    public string? WhatsAppSenderId       { get; set; }
    public bool    WhatsAppEnabled        { get; set; }
    public int     NotificationDelayHours { get; set; }
    public bool    ChatEnabled            { get; set; }
    public int     PatientChatWindowDays  { get; set; }
}

public class TenantSubscriptionResponse
{
    public Guid     SubscriptionId    { get; set; }
    public string   PlanType          { get; set; } = string.Empty;
    public int      CasesAllocated    { get; set; }
    public int      CasesUsed         { get; set; }
    public DateTime BillingCycleStart { get; set; }
    public DateTime BillingCycleEnd   { get; set; }
    public bool     IsActive          { get; set; }

    /// <summary>CasesUsed / CasesAllocated * 100. Returns 0 when CasesAllocated is 0.</summary>
    public decimal UsagePercentage { get; set; }
}

public class TenantResponse
{
    public Guid     TenantId   { get; set; }
    public string   Name       { get; set; } = string.Empty;
    public string   NameAr     { get; set; } = string.Empty;
    public string?  LogoUrl    { get; set; }
    public bool     IsActive   { get; set; }
    public DateTime CreatedAt  { get; set; }

    public TenantSettingsResponse?      Settings            { get; set; }
    public TenantSubscriptionResponse?  ActiveSubscription  { get; set; }
}

public class UpdateTenantSettingsRequest
{
    /// <summary>Accepted values: "Hide", "ShowOptional", "Require". Null = no change.</summary>
    public string? PatientNamePolicy      { get; set; }
    public string? WhatsAppSenderId       { get; set; }
    public bool?   WhatsAppEnabled        { get; set; }
    public int?    NotificationDelayHours { get; set; }
    public bool?   ChatEnabled            { get; set; }
    public int?    PatientChatWindowDays  { get; set; }
}

public class LinkAssistantRequest
{
    [Required]
    public string AssistantId { get; set; } = string.Empty;

    [Required]
    public string PhysicianId { get; set; } = string.Empty;
}

// ─────────────────────────────────────────────────────────────────────────────
// Phase 2 — Referral workflow models
// ─────────────────────────────────────────────────────────────────────────────

public class CreateReferralRequest
{
    /// <summary>
    /// Accepted in the body as a fallback for tooling/tests only.
    /// PhysicianId is always taken from the JWT claim in the controller (Rule 4).
    /// </summary>
    public string? PhysicianId { get; set; }

    /// <summary>Patient's WhatsApp phone number. Format: +967XXXXXXXXX</summary>
    [Required]
    public string PatientPhone { get; set; } = string.Empty;

    public bool WhatsAppDelivery { get; set; } = true;

    /// <summary>Displayed according to tenant PatientNamePolicy.</summary>
    public string? PatientName { get; set; }

    [Required]
    public string AgeGroup { get; set; } = string.Empty;

    [Required]
    public string PrimaryDiagnosis { get; set; } = string.Empty;

    public string? Comorbidities       { get; set; }
    public string? CurrentMedications  { get; set; }
    public string? Allergies           { get; set; }
    public string? MedicalRestrictions { get; set; }

    /// <summary>Override tenant default notification delay in hours. Null = use tenant setting.</summary>
    public int? NotificationDelayHours { get; set; }
}

public class ReferralEngagementResponse
{
    public DateTime? MessageSentAt       { get; set; }
    public DateTime? AppOpenedAt         { get; set; }
    public DateTime? SummaryViewedAt     { get; set; }
    public DateTime? Stage2RequestedAt   { get; set; }
    public DateTime? FeedbackSubmittedAt { get; set; }
}

public class ReferralResponse
{
    public Guid      ReferralId          { get; set; }
    public string    Status              { get; set; } = string.Empty;
    public string?   RiskLevel           { get; set; }
    public string    PatientPhone        { get; set; } = string.Empty;
    public bool      WhatsAppDelivery    { get; set; }
    public DateTime? ScheduledDeliveryAt { get; set; }
    public DateTime? DeliveredAt         { get; set; }
    public DateTime  CreatedAt           { get; set; }
    public string?   SessionId           { get; set; }
    public bool      ChatEnabled         { get; set; }

    public ReferralEngagementResponse? Engagement { get; set; }
}

public class TrackEngagementRequest
{
    /// <summary>Accepted values: "app_opened", "summary_viewed", "stage2_requested".</summary>
    [Required]
    public string EventType { get; set; } = string.Empty;
}

public class TrackArticleEngagementRequest
{
    [Required]
    public string ArticleId { get; set; } = string.Empty;

    /// <summary>
    /// Accepted values: "opened", "depth_25", "depth_50", "depth_75",
    /// "completed", "like", "dislike", "time".
    /// </summary>
    [Required]
    public string EventType { get; set; } = string.Empty;

    public int TimeOnArticleSeconds { get; set; } = 0;

    /// <summary>Required: the referral this article belongs to.</summary>
    [Required]
    public Guid ReferralId { get; set; }
}

public class PatientFeedbackRequest
{
    [Required]
    public bool IsHelpful { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// Phase 2 Task 3 — Engagement detail + article list response models
// ─────────────────────────────────────────────────────────────────────────────

public class ReferralArticleResponse
{
    public string  ArticleId    { get; set; } = string.Empty;
    public string  ArticleType  { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("content_ar")]
    public string  ContentAr    { get; set; } = string.Empty;   // Rule 2 — never "content"

    public string? CoverageCodes { get; set; }
    public int     WordCount     { get; set; }
    public DateTime CreatedAt   { get; set; }
}

public class ArticleEngagementResponse
{
    public string    ArticleId            { get; set; } = string.Empty;
    public DateTime? OpenedAt             { get; set; }
    public DateTime? Depth25At            { get; set; }
    public DateTime? Depth50At            { get; set; }
    public DateTime? Depth75At            { get; set; }
    public DateTime? CompletedAt          { get; set; }
    public int       TimeOnArticleSeconds { get; set; }
    public string    Reaction             { get; set; } = "None";
}

public class PatientFeedbackResponse
{
    public bool      IsHelpful    { get; set; }
    public string?   Comment      { get; set; }
    public DateTime  SubmittedAt  { get; set; }
}

public class ReferralEngagementDetailResponse
{
    public Guid       ReferralId   { get; set; }
    public string     Status       { get; set; } = string.Empty;
    public string?    RiskLevel    { get; set; }
    public string     PatientPhone { get; set; } = string.Empty;

    public ReferralEngagementResponse?      Timeline  { get; set; }
    public List<ArticleEngagementResponse>  Articles  { get; set; } = [];
    public PatientFeedbackResponse?         Feedback  { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// Phase 3 Task 1 — Quality System models
// ─────────────────────────────────────────────────────────────────────────────

public class CreateTestScenarioRequest
{
    [Required]
    public string AgeGroup { get; set; } = string.Empty;

    [Required]
    public string PrimaryDiagnosis { get; set; } = string.Empty;

    public string? Comorbidities      { get; set; }
    public string? CurrentMedications { get; set; }
    public string? Allergies          { get; set; }
    public string? MedicalRestrictions { get; set; }

    /// <summary>Optional label — stored in PatientDataJson, never used for real patient data.</summary>
    public string? TestPatientName { get; set; }
}

public class TestScenarioResponse
{
    public Guid     ScenarioId           { get; set; }
    public string   PhysicianId          { get; set; } = string.Empty;
    public Guid     TenantId             { get; set; }
    public string   Status               { get; set; } = string.Empty;
    public string   PatientDataJson      { get; set; } = string.Empty;
    public string?  GeneratedContentJson  { get; set; }
    public string?  GeneratedArticlesJson { get; set; }
    public DateTime CreatedAt            { get; set; }
    public ContentEvaluationResponse? Evaluation { get; set; }
}

public class SubmitEvaluationRequest
{
    [Required][Range(1, 5)] public int AccuracyRating     { get; set; }
    [Required][Range(1, 5)] public int ClarityRating      { get; set; }
    [Required][Range(1, 5)] public int RelevanceRating    { get; set; }
    [Required][Range(1, 5)] public int CompletenessRating { get; set; }

    public bool IsAppropriate         { get; set; }
    public bool IsCulturallySensitive { get; set; }
    public bool IsArabicQuality       { get; set; }

    [StringLength(2000)] public string? WhatWorked       { get; set; }
    [StringLength(2000)] public string? NeedsImprovement { get; set; }
    [StringLength(2000)] public string? Comments         { get; set; }
}

public class ContentEvaluationResponse
{
    public Guid   EvaluationId  { get; set; }
    public Guid   ScenarioId    { get; set; }
    public string PhysicianId   { get; set; } = string.Empty;

    public int  AccuracyRating      { get; set; }
    public int  ClarityRating       { get; set; }
    public int  RelevanceRating     { get; set; }
    public int  CompletenessRating  { get; set; }

    public bool IsAppropriate         { get; set; }
    public bool IsCulturallySensitive { get; set; }
    public bool IsArabicQuality       { get; set; }

    public string? WhatWorked       { get; set; }
    public string? NeedsImprovement { get; set; }
    public string? Comments         { get; set; }

    public DateTime SubmittedAt { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// Phase 3 Task 2 — Physician-Patient Async Chat models
// ─────────────────────────────────────────────────────────────────────────────

public class SendMessageRequest
{
    [Required]
    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;
}

public class ChatMessageResponse
{
    public Guid     MessageId  { get; set; }
    public string   SenderRole { get; set; } = string.Empty;   // "Physician" or "Patient"
    public string   Content    { get; set; } = string.Empty;
    public DateTime SentAt     { get; set; }
    public bool     IsRead     { get; set; }
}

public class ChatThreadResponse
{
    public Guid     ThreadId     { get; set; }
    public Guid     ReferralId   { get; set; }
    public bool     IsEnabled    { get; set; }
    public DateTime ExpiresAt    { get; set; }

    /// <summary>Calculated: ExpiresAt &lt; DateTime.UtcNow.</summary>
    public bool IsExpired    { get; set; }
    public int  MessageCount { get; set; }

    public List<ChatMessageResponse> Messages { get; set; } = [];

    /// <summary>Always included in every response regardless of caller role.</summary>
    public string DisclaimerAr { get; set; } =
        "هذه القناة مخصصة لتوضيح محتوى التثقيف الصحي فقط. " +
        "لا تمثل استشارة طبية. للحصول على مشورة طبية أو في " +
        "حالات الطوارئ، تواصل مع عيادة طبيبك مباشرة.";

    /// <summary>Always included in every response regardless of caller role.</summary>
    public string DisclaimerEn { get; set; } =
        "This channel is for clarification of your health education " +
        "content only. It does not constitute a medical consultation. " +
        "For medical advice or emergencies, contact your physician's " +
        "clinic directly.";
}

public class UpdateChatSettingsRequest
{
    public bool ChatEnabled { get; set; }
}

public class UserSummaryResponse
{
    public Guid     UserId    { get; set; }
    public string   Email     { get; set; } = string.Empty;
    public string   FullName  { get; set; } = string.Empty;
    public string   Role      { get; set; } = string.Empty;
    public Guid?    TenantId  { get; set; }
    public bool     IsActive  { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateTenantUserRequest
{
    [Required] public string  Email       { get; set; } = string.Empty;
    [Required] public string  FullName    { get; set; } = string.Empty;
    [Required] public string  Password    { get; set; } = string.Empty;
    [Required] public string  Role        { get; set; } = string.Empty;
    public            string? Specialty   { get; set; }
    public            string? Institution { get; set; }
}

public class GenerateArticleResponse
{
    public string Content { get; set; } = string.Empty;
}

public class AssistantLinkResponse
{
    public Guid     LinkId        { get; set; }
    public string   AssistantId   { get; set; } = string.Empty;
    public string   AssistantName { get; set; } = string.Empty;
    public string   PhysicianId   { get; set; } = string.Empty;
    public string   PhysicianName { get; set; } = string.Empty;
    public Guid     TenantId      { get; set; }
    public bool     IsActive      { get; set; }
    public DateTime LinkedAt      { get; set; }
}
