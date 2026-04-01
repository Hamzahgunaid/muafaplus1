using MuafaPlus.Data;
using MuafaPlus.Models;

namespace MuafaPlus.Services;

/// <summary>
/// Phase 3: Stage 2 now enqueued as a Hangfire background job.
/// ExecuteCompleteWorkflowAsync runs Stage 1 synchronously then enqueues Stage 2,
/// returning AsyncWorkflowResult immediately. Frontend polls /Session/{id}/status.
/// </summary>
public class WorkflowService
{
    private readonly MuafaApiClient _apiClient;
    private readonly RiskCalculatorService _riskCalculator;
    private readonly MuafaDbContext _db;
    private readonly ILogger<WorkflowService> _logger;

    public WorkflowService(
        MuafaApiClient apiClient,
        RiskCalculatorService riskCalculator,
        MuafaDbContext db,
        ILogger<WorkflowService> logger)
    {
        _apiClient      = apiClient;
        _riskCalculator = riskCalculator;
        _db             = db;
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
                SessionId = sessionId, PatientId = patientId,
                PhysicianId = physicianId, Stage = "stage_1",
                Status = "in_progress", RiskLevel = riskScore.RiskLevelString
            };
            _db.GenerationSessions.Add(session);
            await _db.SaveChangesAsync();

            var stage1 = await _apiClient.GenerateStage1Async(patientData, riskScore);
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
            return new AsyncWorkflowResult { Success = false, SessionId = sessionId, ErrorMessage = ex.Message };
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
            SessionId = sessionId, PatientId = patientId, PhysicianId = physicianId,
            Stage = "stage_1", Status = "in_progress", RiskLevel = riskScore.RiskLevelString
        };
        _db.GenerationSessions.Add(session);
        await _db.SaveChangesAsync();

        var result = await _apiClient.GenerateStage1Async(patientData, riskScore);

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

    private async Task CreatePatientAsync(string physicianId, PatientData data, string patientId)
    {
        _db.Patients.Add(new Patient
        {
            PatientId = patientId, PhysicianId = physicianId,
            PrimaryDiagnosis = data.PrimaryDiagnosis, AgeGroup = data.AgeGroup,
            Comorbidities = data.Comorbidities, CurrentMedications = data.CurrentMedications,
            Allergies = data.Allergies, MedicalRestrictions = data.MedicalRestrictions
        });
        await _db.SaveChangesAsync();
    }

    private async Task SaveArticleAsync(string sessionId, string type,
        string content, TokenUsage usage, string codes)
    {
        _db.GeneratedArticles.Add(new GeneratedArticle
        {
            SessionId = sessionId, ArticleType = type, CoverageCodes = codes,
            Content = content,
            WordCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
            TokensInput = usage.InputTokens, TokensOutput = usage.OutputTokens,
            CostUsd = usage.CalculateCost()
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
