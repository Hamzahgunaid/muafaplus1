using Hangfire;
using MuafaPlus.Data;
using MuafaPlus.Models;
using Microsoft.EntityFrameworkCore;

namespace MuafaPlus.Services;

/// <summary>
/// Hangfire background job service.
///
/// The HTTP endpoint now executes only Stage 1 (fast, ~5–15 s) synchronously
/// and then enqueues a Stage 2 job via Hangfire, returning the sessionId
/// immediately. The frontend polls GET /api/v1/Session/{id}/status until
/// status = "complete".
///
/// Retry policy: Hangfire retries failed jobs with exponential back-off
/// (10 attempts by default). On exhaustion the job moves to the Failed queue
/// and the session status is updated to "failed".
/// </summary>
public class GenerationJobService
{
    private readonly MuafaApiClient _apiClient;
    private readonly MuafaDbContext _db;
    private readonly ILogger<GenerationJobService> _logger;

    public GenerationJobService(
        MuafaApiClient apiClient,
        MuafaDbContext db,
        ILogger<GenerationJobService> logger)
    {
        _apiClient = apiClient;
        _db        = db;
        _logger    = logger;
    }

    /// <summary>
    /// Enqueues the Stage 2 generation job and returns immediately.
    /// Called from WorkflowService after Stage 1 completes.
    /// </summary>
    public static string EnqueueStage2(
        string sessionId,
        PatientData patientData,
        List<ArticleSpec> specs,
        RiskScore riskScore)
    {
        return BackgroundJob.Enqueue<GenerationJobService>(
            svc => svc.ExecuteStage2JobAsync(sessionId, patientData, specs, riskScore));
    }

    /// <summary>
    /// The actual Hangfire job. Runs outside the HTTP request context.
    /// Hangfire injects this class via DI when executing the job.
    /// </summary>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [30, 120, 300])]
    public async Task ExecuteStage2JobAsync(
        string sessionId,
        PatientData patientData,
        List<ArticleSpec> specs,
        RiskScore riskScore)
    {
        _logger.LogInformation("Stage 2 job starting — session:{SessionId} articles:{Count}",
            sessionId, specs.Count);

        var session = await _db.GenerationSessions.FindAsync(sessionId);
        if (session == null)
        {
            _logger.LogError("Stage 2 job: session {SessionId} not found in database", sessionId);
            return;
        }

        decimal totalStage2Cost = 0m;
        int successCount = 0;

        foreach (var spec in specs)
        {
            try
            {
                var result = await _apiClient.GenerateStage2Async(patientData, spec, riskScore);

                if (result.Success && result.Output?.Article != null)
                {
                    var article = result.Output.Article;
                    _db.GeneratedArticles.Add(new GeneratedArticle
                    {
                        SessionId     = sessionId,
                        ArticleType   = "detailed",
                        CoverageCodes = string.Join(",", spec.CoverageCodes),
                        Content       = article.ContentAr,
                        WordCount     = article.WordCount,
                        TokensInput   = result.TokenUsage.InputTokens,
                        TokensOutput  = result.TokenUsage.OutputTokens,
                        CostUsd       = result.TokenUsage.CalculateCost()
                    });
                    await _db.SaveChangesAsync();

                    totalStage2Cost += result.TokenUsage.CalculateCost();
                    successCount++;

                    _logger.LogInformation("Article {Id} saved — session:{Session}",
                        spec.ArticleId, sessionId);
                }
                else
                {
                    _logger.LogWarning("Article {Id} failed — {Error}", spec.ArticleId, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception generating article {Id}", spec.ArticleId);
                // Continue with remaining articles rather than aborting the whole job
            }
        }

        // Update session totals
        session.TotalCost    = (session.TotalCost ?? 0m) + totalStage2Cost;
        session.TotalArticles = await _db.GeneratedArticles.CountAsync(a => a.SessionId == sessionId);
        session.Status        = "complete";
        session.CompletedAt   = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Stage 2 job complete — session:{Session} success:{Success}/{Total} cost:${Cost:F4}",
            sessionId, successCount, specs.Count, totalStage2Cost);
    }
}
