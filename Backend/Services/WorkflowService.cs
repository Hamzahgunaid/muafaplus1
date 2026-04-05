using System.Text.Json;
using MuafaPlus.Data;
using MuafaPlus.Models;

namespace MuafaPlus.Services;

/// <summary>
/// Phase 2 Task 4: ArticleLibrary (Layer 1) cache check added around Stage 1.
/// Before every Claude API call, the profile hash is checked against ArticleLibrary.
/// After every generation, the Stage1Output is persisted to ArticleLibrary.
///
/// Phase 3: Stage 2 now enqueued as a Hangfire background job.
/// ExecuteCompleteWorkflowAsync runs Stage 1 synchronously then enqueues Stage 2,
/// returning AsyncWorkflowResult immediately. Frontend polls /Session/{id}/status.
/// </summary>
public class WorkflowService
{
    private readonly MuafaApiClient         _apiClient;
    private readonly RiskCalculatorService  _riskCalculator;
    private readonly MuafaDbContext         _db;
    private readonly ProfileHashService     _profileHash;
    private readonly ArticleLibraryService  _libraryService;
    private readonly ILogger<WorkflowService> _logger;

    private static readonly JsonSerializerOptions _jsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    public WorkflowService(
        MuafaApiClient          apiClient,
        RiskCalculatorService   riskCalculator,
        MuafaDbContext          db,
        ProfileHashService      profileHash,
        ArticleLibraryService   libraryService,
        ILogger<WorkflowService> logger)
    {
        _apiClient      = apiClient;
        _riskCalculator = riskCalculator;
        _db             = db;
        _profileHash    = profileHash;
        _libraryService = libraryService;
        _logger         = logger;
    }

    public async Task<AsyncWorkflowResult> ExecuteCompleteWorkflowAsync(
        string physicianId, PatientData patientData)
    {
        var sessionId = Guid.NewGuid().ToString();
        var patientId = Guid.NewGuid().ToString();

        _logger.LogInformation("Workflow {S} starting — physician {P}", sessionId, physicianId);

        try
        {
            var riskScore = _riskCalculator.Calculate(patientData);
            _logger.LogInformation("Risk: {L} ({S})", riskScore.RiskLevelString, riskScore.TotalScore);

            await CreatePatientAsync(physicianId, patientData, patientId);

            var session = new GenerationSession
            {
                SessionId   = sessionId, PatientId = patientId,
                PhysicianId = physicianId, Stage    = "stage_1",
                Status      = "in_progress", RiskLevel = riskScore.RiskLevelString
            };
            _db.GenerationSessions.Add(session);
            await _db.SaveChangesAsync();

            // ── Layer 1: ArticleLibrary check ─────────────────────────────────
            var profileHash = _profileHash.GenerateHash(patientData);
            var stage1      = await GetOrGenerateStage1Async(patientData, riskScore, profileHash);
            // ─────────────────────────────────────────────────────────────────

            if (!stage1.Success || stage1.Output == null)
                throw new InvalidOperationException($"Stage 1 failed: {stage1.ErrorMessage}");

            await SaveArticleAsync(sessionId, "summary", stage1.Output.SummaryArticle,
                stage1.TokenUsage, "understanding_condition,safety_preparedness");

            session.Stage     = "stage_2";
            session.TotalCost = stage1.TokenUsage.CalculateCost();
            await _db.SaveChangesAsync();

            var jobId = GenerationJobService.EnqueueStage2(
                sessionId, patientData, stage1.Output.ArticleOutlines, riskScore);

            _logger.LogInformation("Stage 2 enqueued — session:{S} job:{J} articles:{C}",
                sessionId, jobId, stage1.Output.ArticleOutlines.Count);

            return new AsyncWorkflowResult
            {
                Success        = true,
                SessionId      = sessionId,
                PatientId      = patientId,
                HangfireJobId  = jobId,
                RiskScore      = riskScore,
                SummaryArticle = stage1.Output.SummaryArticle,
                ArticleCount   = stage1.Output.ArticleOutlines.Count,
                Stage1Cost     = stage1.TokenUsage.CalculateCost()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Workflow {S} failed", sessionId);
            var session = await _db.GenerationSessions.FindAsync(sessionId);
            if (session != null)
            {
                session.Status       = "failed";
                session.ErrorMessage = ex.Message[..Math.Min(ex.Message.Length, 499)];
                session.CompletedAt  = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
            return new AsyncWorkflowResult
            {
                Success      = false,
                SessionId    = sessionId,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<Stage1Result> ExecuteStage1OnlyAsync(string physicianId, PatientData patientData)
    {
        var sessionId = Guid.NewGuid().ToString();
        var patientId = Guid.NewGuid().ToString();
        var riskScore = _riskCalculator.Calculate(patientData);

        await CreatePatientAsync(physicianId, patientData, patientId);

        var session = new GenerationSession
        {
            SessionId   = sessionId, PatientId   = patientId,
            PhysicianId = physicianId, Stage      = "stage_1",
            Status      = "in_progress", RiskLevel = riskScore.RiskLevelString
        };
        _db.GenerationSessions.Add(session);
        await _db.SaveChangesAsync();

        // ── Layer 1: ArticleLibrary check ─────────────────────────────────────
        var profileHash = _profileHash.GenerateHash(patientData);
        var result      = await GetOrGenerateStage1Async(patientData, riskScore, profileHash);
        // ─────────────────────────────────────────────────────────────────────

        if (result.Success && result.Output != null)
        {
            session.Status        = "complete";
            session.TotalArticles = result.Output.ArticleOutlines.Count;
            session.TotalCost     = result.TokenUsage.CalculateCost();
            await SaveArticleAsync(sessionId, "summary", result.Output.SummaryArticle,
                result.TokenUsage, "understanding_condition,safety_preparedness");
        }
        else
        {
            session.Status       = "failed";
            session.ErrorMessage = result.ErrorMessage?[..Math.Min(result.ErrorMessage?.Length ?? 0, 499)];
        }
        session.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return result;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Layer 1 cache helper
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Checks ArticleLibrary before calling the Claude API.
    ///
    /// Cache HIT:  deserialise stored Stage1Output, return with zero TokenUsage
    ///             (cost = $0.00, no API call made).
    /// Cache MISS: call Claude API, serialise Stage1Output, persist to library.
    /// </summary>
    private async Task<Stage1Result> GetOrGenerateStage1Async(
        PatientData patientData,
        RiskScore   riskScore,
        string      profileHash)
    {
        // ── Check library ────────────────────────────────────────────────────
        var cached = await _libraryService.GetByHashAsync(profileHash);

        if (cached != null)
        {
            _logger.LogInformation(
                "CACHE HIT — skipping Claude API call for hash:{Hash}", profileHash);

            var cachedOutput = JsonSerializer.Deserialize<Stage1Output>(cached, _jsonOpts);
            return new Stage1Result
            {
                Success    = true,
                Output     = cachedOutput,
                TokenUsage = new TokenUsage(),   // $0 — no API call
                Model      = "library-cache"
            };
        }

        // ── Cache miss — call Claude API ──────────────────────────────────────
        var stage1 = await _apiClient.GenerateStage1Async(patientData, riskScore);

        if (stage1.Success && stage1.Output != null)
        {
            // Persist to library for future identical profiles
            var outputJson = JsonSerializer.Serialize(stage1.Output);
            await _libraryService.SaveAsync(profileHash, outputJson, tenantId: null);
        }

        return stage1;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────────

    private async Task CreatePatientAsync(string physicianId, PatientData data, string patientId)
    {
        _db.Patients.Add(new Patient
        {
            PatientId        = patientId, PhysicianId   = physicianId,
            PrimaryDiagnosis = data.PrimaryDiagnosis,  AgeGroup = data.AgeGroup,
            Comorbidities    = data.Comorbidities,
            CurrentMedications = data.CurrentMedications,
            Allergies          = data.Allergies,
            MedicalRestrictions = data.MedicalRestrictions
        });
        await _db.SaveChangesAsync();
    }

    private async Task SaveArticleAsync(string sessionId, string type,
        string content, TokenUsage usage, string codes)
    {
        _db.GeneratedArticles.Add(new GeneratedArticle
        {
            SessionId   = sessionId, ArticleType = type, CoverageCodes = codes,
            Content     = content,
            WordCount   = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
            TokensInput = usage.InputTokens, TokensOutput = usage.OutputTokens,
            CostUsd     = usage.CalculateCost()
        });
        await _db.SaveChangesAsync();
    }
}

public class AsyncWorkflowResult
{
    public bool       Success        { get; set; }
    public string     SessionId      { get; set; } = string.Empty;
    public string     PatientId      { get; set; } = string.Empty;
    public string?    HangfireJobId  { get; set; }
    public RiskScore? RiskScore      { get; set; }
    public string?    SummaryArticle { get; set; }
    public int        ArticleCount   { get; set; }
    public decimal    Stage1Cost     { get; set; }
    public string?    ErrorMessage   { get; set; }
}
