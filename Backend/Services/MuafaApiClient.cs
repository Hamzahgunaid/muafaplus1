using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using MuafaPlus.Models;
using System.Text.Json;

namespace MuafaPlus.Services;

/// <summary>
/// Client for interacting with the Claude API via Anthropic.SDK v5.x.
///
/// Prompt caching (SDK v5):
///   - MessageParameters.System is List&lt;SystemMessage&gt; (not a plain string).
///   - SystemMessage(text, CacheControl) marks the block for server-side caching.
///   - PromptCaching = PromptCacheType.FineGrained activates per-block cache_control.
///   - Usage exposes CacheCreationInputTokens and CacheReadInputTokens.
///   - Cache TTL is ~5 minutes (ephemeral). Stage 2 articles within the same session
///     reuse the cached system prompt, cutting input-token cost by ~10×.
/// </summary>
public class MuafaApiClient
{
    private readonly AnthropicClient _client;
    private readonly PromptBuilder _promptBuilder;
    private readonly ILogger<MuafaApiClient> _logger;

    // Model and generation parameters — read from configuration so they can be
    // overridden per-environment without code changes.
    private readonly string _model;
    private readonly int _maxTokens;

    public MuafaApiClient(
        IConfiguration configuration,
        PromptBuilder promptBuilder,
        ILogger<MuafaApiClient> logger)
    {
        _promptBuilder = promptBuilder;
        _logger = logger;

        var apiKey = configuration["Anthropic:ApiKey"]
            ?? throw new InvalidOperationException(
                "Anthropic API key is not configured. " +
                "Set it via user secrets: dotnet user-secrets set \"Anthropic:ApiKey\" \"sk-ant-...\"");

        _client    = new AnthropicClient(apiKey);
        _model     = configuration["Anthropic:Model"]     ?? "claude-sonnet-4-20250514";
        _maxTokens = int.TryParse(configuration["Anthropic:MaxTokens"], out var mt) ? mt : 4096;

        _logger.LogInformation("MuafaApiClient initialised — model: {Model}, max_tokens: {MaxTokens}",
            _model, _maxTokens);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Stage 1
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Stage 1: Risk assessment echo + summary article + article outlines.
    /// The risk has already been computed in C# (RiskCalculatorService) and is
    /// embedded in the user prompt via riskScore.ToPromptBlock().
    /// </summary>
    public async Task<Stage1Result> GenerateStage1Async(
        PatientData patientData,
        RiskScore riskScore)
    {
        try
        {
            _logger.LogInformation(
                "Stage 1 starting — diagnosis: {Diagnosis}, risk: {Risk}",
                patientData.PrimaryDiagnosis, riskScore.RiskLevelString);

            var systemPrompt = _promptBuilder.GetStage1SystemPrompt();
            var userPrompt   = _promptBuilder.BuildStage1UserPrompt(patientData, riskScore);

            var parameters = new MessageParameters
            {
                Model         = _model,
                MaxTokens     = _maxTokens,
                PromptCaching = PromptCacheType.FineGrained,
                System        = [new SystemMessage(systemPrompt, new CacheControl { Type = CacheControlType.ephemeral })],
                Messages      = [new Message(RoleType.User, userPrompt)]
            };

            var response = await _client.Messages.GetClaudeMessageAsync(parameters);
            var rawJson  = ExtractText(response);

            var output = DeserializeOrThrow<Stage1Output>(rawJson, "Stage 1");

            var usage = MapUsage(response.Usage);
            _logger.LogInformation(
                "Stage 1 complete — in:{In} out:{Out} cacheWrite:{CW} cacheRead:{CR} cost:${Cost:F4}",
                usage.InputTokens, usage.OutputTokens,
                usage.CacheCreationTokens, usage.CacheReadTokens, usage.CalculateCost());

            return new Stage1Result
            {
                Success    = true,
                Output     = output,
                TokenUsage = usage,
                Model      = response.Model
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stage 1 failed");
            return new Stage1Result
            {
                Success      = false,
                ErrorMessage = ex.Message,
                ErrorType    = ex.GetType().Name
            };
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Stage 2
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Stage 2: Detailed article generation for a single ArticleSpec.
    /// </summary>
    public async Task<Stage2Result> GenerateStage2Async(
        PatientData patientData,
        ArticleSpec articleSpec,
        RiskScore riskScore)
    {
        try
        {
            _logger.LogInformation(
                "Stage 2 starting — article: {ArticleId}", articleSpec.ArticleId);

            var systemPrompt = _promptBuilder.GetStage2SystemPrompt();
            var userPrompt   = _promptBuilder.BuildStage2UserPrompt(patientData, articleSpec, riskScore);

            var parameters = new MessageParameters
            {
                Model         = _model,
                MaxTokens     = _maxTokens,
                PromptCaching = PromptCacheType.FineGrained,
                System        = [new SystemMessage(systemPrompt, new CacheControl { Type = CacheControlType.ephemeral })],
                Messages      = [new Message(RoleType.User, userPrompt)]
            };

            var response = await _client.Messages.GetClaudeMessageAsync(parameters);
            var rawJson  = ExtractText(response);

            var output = DeserializeOrThrow<Stage2Output>(rawJson, $"Stage 2 [{articleSpec.ArticleId}]");

            var usage = MapUsage(response.Usage);
            _logger.LogInformation(
                "Stage 2 complete — article:{Id} in:{In} out:{Out} cacheWrite:{CW} cacheRead:{CR} cost:${Cost:F4}",
                articleSpec.ArticleId, usage.InputTokens, usage.OutputTokens,
                usage.CacheCreationTokens, usage.CacheReadTokens, usage.CalculateCost());

            return new Stage2Result
            {
                Success    = true,
                Output     = output,
                TokenUsage = usage,
                Model      = response.Model
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stage 2 failed — article: {ArticleId}", articleSpec.ArticleId);
            return new Stage2Result
            {
                Success      = false,
                ErrorMessage = ex.Message,
                ErrorType    = ex.GetType().Name
            };
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static string ExtractText(MessageResponse response)
    {
        var text = response.Content
            .OfType<TextContent>()
            .Select(c => c.Text)
            .FirstOrDefault() ?? string.Empty;

        // Strip markdown code fences if the model wrapped the JSON
        if (text.StartsWith("```"))
        {
            var firstNewline = text.IndexOf('\n');
            var lastFence    = text.LastIndexOf("```");
            if (firstNewline >= 0 && lastFence > firstNewline)
                text = text[(firstNewline + 1)..lastFence].Trim();
        }

        return text;
    }

    private static T DeserializeOrThrow<T>(string json, string stage)
    {
        var result = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (result == null)
            throw new InvalidOperationException($"Failed to deserialise {stage} response. Raw JSON was empty or null.");

        return result;
    }

    private static TokenUsage MapUsage(Anthropic.SDK.Messaging.Usage sdkUsage) => new()
    {
        InputTokens         = sdkUsage.InputTokens,
        OutputTokens        = sdkUsage.OutputTokens,
        CacheCreationTokens = sdkUsage.CacheCreationInputTokens,
        CacheReadTokens     = sdkUsage.CacheReadInputTokens,
    };
}
