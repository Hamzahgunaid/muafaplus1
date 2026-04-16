using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MuafaPlus.Data;
using MuafaPlus.Models;
using MuafaPlus.Services;

namespace MuafaPlus.Controllers;

/// <summary>
/// Phase 3 Task 1 — Quality System.
/// Allows physicians to create synthetic test scenarios, generate Stage 1 content,
/// evaluate the output, and stream preview content over SSE.
///
/// Rules in effect:
/// RULE 1  — Risk calculated in C# before every AI call (RiskCalculatorService)
/// RULE 4  — PhysicianId always from JWT
/// RULE 6  — All endpoints require [Authorize]
/// RULE 11 — All non-SSE endpoints return ApiResponse<T>
/// RULE 12 — ArticleLibrary checked before every Claude API call (via WorkflowService)
/// </summary>
[ApiController]
[Route("api/v1/test-scenarios")]
[Authorize]
[Produces("application/json")]
public class TestScenariosController : ControllerBase
{
    private readonly MuafaDbContext              _db;
    private readonly WorkflowService             _workflow;
    private readonly MuafaApiClient              _apiClient;
    private readonly RiskCalculatorService       _riskCalculator;
    private readonly ILogger<TestScenariosController> _logger;

    private static readonly JsonSerializerOptions _jsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    public TestScenariosController(
        MuafaDbContext                   db,
        WorkflowService                  workflow,
        MuafaApiClient                   apiClient,
        RiskCalculatorService            riskCalculator,
        ILogger<TestScenariosController> logger)
    {
        _db             = db;
        _workflow       = workflow;
        _apiClient      = apiClient;
        _riskCalculator = riskCalculator;
        _logger         = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/v1/test-scenarios
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a TestScenario and immediately generates Stage 1 content.
    /// Runs WorkflowService.ExecuteStage1OnlyAsync (ArticleLibrary cache checked — Rule 12).
    /// Returns 201 Created with generated content included.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TestScenarioResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>),               StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<TestScenarioResponse>>> Create(
        [FromBody] CreateTestScenarioRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ValidationErrorResponse());

        var physicianId = User.FindFirst(ClaimNames.PhysicianId)?.Value;
        if (string.IsNullOrEmpty(physicianId))
            return Unauthorized(new ApiResponse<object>
            {
                Success = false, Error = "لم يتم التحقق من هوية الطبيب.", ErrorType = "MissingPhysicianClaim"
            });

        var tenantIdClaim = User.FindFirst("TenantId")?.Value;
        if (!Guid.TryParse(tenantIdClaim, out var tenantId) || tenantId == Guid.Empty)
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Error   = "Physician is not linked to a tenant.",
                ErrorType = "TenantNotLinked"
            });

        var patientData = BuildPatientData(request);

        try
        {
            // Stage 1 only — evaluation scenarios don't trigger Stage 2 (Rule 3)
            var result = await _workflow.ExecuteStage1OnlyAsync(physicianId, patientData);

            if (!result.Success || result.Output == null)
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
                {
                    Success = false, Error = result.ErrorMessage ?? "Stage 1 generation failed",
                    ErrorType = "GenerationFailed"
                });

            var scenario = new TestScenario
            {
                PhysicianId          = physicianId,
                TenantId             = tenantId,
                PatientDataJson      = JsonSerializer.Serialize(patientData),
                GeneratedContentJson = JsonSerializer.Serialize(result.Output),
                Status               = TestScenarioStatus.Generated,
                CreatedAt            = DateTime.UtcNow
            };

            _db.TestScenarios.Add(scenario);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "TestScenario {ScenarioId} created — physician:{PhysicianId} tenant:{TenantId}",
                scenario.ScenarioId, physicianId, tenantId);

            return CreatedAtAction(
                nameof(GetById),
                new { id = scenario.ScenarioId },
                new ApiResponse<TestScenarioResponse>
                {
                    Success = true,
                    Data    = MapToResponse(scenario)
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TestScenario Create error — physician:{PhysicianId}", physicianId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false, Error = "Internal server error", ErrorType = ex.GetType().Name
            });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/v1/test-scenarios
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all test scenarios for the authenticated physician, newest first.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<TestScenarioResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<TestScenarioResponse>>>> GetTestScenarios()
    {
        var physicianId = User.FindFirst(ClaimNames.PhysicianId)?.Value;
        if (string.IsNullOrEmpty(physicianId))
            return Unauthorized(new ApiResponse<List<TestScenarioResponse>>
            {
                Success = false, Error = "غير مصرح", ErrorType = "MissingPhysicianClaim"
            });

        var scenarios = await _db.TestScenarios
            .Include(s => s.Evaluation)
            .Where(s => s.PhysicianId == physicianId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return Ok(new ApiResponse<List<TestScenarioResponse>>
        {
            Success = true,
            Data    = scenarios.Select(MapToResponse).ToList()
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/v1/test-scenarios/{id}
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a single TestScenario with deserialized patient data, generated
    /// content, and evaluation (if submitted). Returns 404 if not found or
    /// not owned by the current physician.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TestScenarioResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>),               StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TestScenarioResponse>>> GetById(Guid id)
    {
        var physicianId = User.FindFirst(ClaimNames.PhysicianId)?.Value;
        if (string.IsNullOrEmpty(physicianId))
            return Unauthorized(new ApiResponse<object>
            {
                Success = false, Error = "لم يتم التحقق من هوية الطبيب.", ErrorType = "MissingPhysicianClaim"
            });

        var scenario = await _db.TestScenarios
            .Include(s => s.Evaluation)
            .FirstOrDefaultAsync(s => s.ScenarioId == id && s.PhysicianId == physicianId);

        if (scenario == null)
            return NotFound(new ApiResponse<object>
            {
                Success = false, Error = $"TestScenario {id} not found.", ErrorType = "NotFound"
            });

        return Ok(new ApiResponse<TestScenarioResponse> { Success = true, Data = MapToResponse(scenario) });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/v1/test-scenarios/{id}/evaluation
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Submits a quality evaluation for a Generated scenario.
    /// Returns 400 if scenario is not in Generated status.
    /// Returns 409 if evaluation already exists.
    /// Updates scenario Status to Evaluated.
    /// </summary>
    [HttpPost("{id:guid}/evaluation")]
    [ProducesResponseType(typeof(ApiResponse<ContentEvaluationResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>),                    StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>),                    StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>),                    StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ContentEvaluationResponse>>> SubmitEvaluation(
        Guid id, [FromBody] SubmitEvaluationRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ValidationErrorResponse());

        var physicianId = User.FindFirst(ClaimNames.PhysicianId)?.Value;
        if (string.IsNullOrEmpty(physicianId))
            return Unauthorized(new ApiResponse<object>
            {
                Success = false, Error = "لم يتم التحقق من هوية الطبيب.", ErrorType = "MissingPhysicianClaim"
            });

        var scenario = await _db.TestScenarios
            .Include(s => s.Evaluation)
            .FirstOrDefaultAsync(s => s.ScenarioId == id && s.PhysicianId == physicianId);

        if (scenario == null)
            return NotFound(new ApiResponse<object>
            {
                Success = false, Error = $"TestScenario {id} not found.", ErrorType = "NotFound"
            });

        if (scenario.Status != TestScenarioStatus.Generated)
            return BadRequest(new ApiResponse<object>
            {
                Success   = false,
                Error     = "Scenario must be in Generated status before evaluation can be submitted.",
                ErrorType = "InvalidStatus"
            });

        if (scenario.Evaluation != null)
            return Conflict(new ApiResponse<object>
            {
                Success   = false,
                Error     = "Evaluation already exists for this scenario.",
                ErrorType = "Conflict"
            });

        var evaluation = new ContentEvaluation
        {
            ScenarioId            = id,
            PhysicianId           = physicianId,
            AccuracyRating        = request.AccuracyRating,
            ClarityRating         = request.ClarityRating,
            RelevanceRating       = request.RelevanceRating,
            CompletenessRating    = request.CompletenessRating,
            IsAppropriate         = request.IsAppropriate,
            IsCulturallySensitive = request.IsCulturallySensitive,
            IsArabicQuality       = request.IsArabicQuality,
            WhatWorked            = request.WhatWorked,
            NeedsImprovement      = request.NeedsImprovement,
            Comments              = request.Comments,
            SubmittedAt           = DateTime.UtcNow
        };

        _db.ContentEvaluations.Add(evaluation);
        scenario.Status = TestScenarioStatus.Evaluated;
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "ContentEvaluation {EvaluationId} submitted — scenario:{ScenarioId} physician:{PhysicianId}",
            evaluation.EvaluationId, id, physicianId);

        return Created(
            $"/api/v1/test-scenarios/{id}/evaluation",
            new ApiResponse<ContentEvaluationResponse>
            {
                Success = true,
                Data    = MapEvalToResponse(evaluation)
            });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/v1/test-scenarios/generate/stream
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// SSE preview endpoint — generates Stage 1 content and streams the result.
    /// Does NOT save a TestScenario record (preview only).
    /// Does NOT use Hangfire — runs synchronously on the HTTP thread.
    /// ArticleLibrary checked before every Claude call (Rule 12).
    /// Risk calculator runs in C# before every call (Rule 1).
    ///
    /// SSE events:
    ///   data: {stage1OutputJson}\n\n
    ///   data: [DONE]\n\n
    /// </summary>
    [HttpPost("generate/stream")]
    public async Task GenerateStream([FromBody] CreateTestScenarioRequest request)
    {
        Response.Headers["Content-Type"]      = "text/event-stream";
        Response.Headers["Cache-Control"]     = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";
        Response.Headers["Connection"]        = "keep-alive";

        var physicianId = User.FindFirst(ClaimNames.PhysicianId)?.Value;
        if (string.IsNullOrEmpty(physicianId))
        {
            await Response.WriteAsync("data: {\"error\":\"Unauthorized\"}\n\n");
            await Response.WriteAsync("data: [DONE]\n\n");
            await Response.Body.FlushAsync();
            return;
        }

        if (string.IsNullOrWhiteSpace(request.AgeGroup) ||
            string.IsNullOrWhiteSpace(request.PrimaryDiagnosis))
        {
            await Response.WriteAsync("data: {\"error\":\"AgeGroup and PrimaryDiagnosis are required\"}\n\n");
            await Response.WriteAsync("data: [DONE]\n\n");
            await Response.Body.FlushAsync();
            return;
        }

        var patientData = BuildPatientData(request);

        try
        {
            // Rule 1 + Rule 12 handled inside ExecuteStage1OnlyAsync
            var result = await _workflow.ExecuteStage1OnlyAsync(physicianId, patientData);

            if (result.Success && result.Output != null)
            {
                var json = JsonSerializer.Serialize(result.Output);
                await Response.WriteAsync($"data: {json}\n\n");
            }
            else
            {
                var errorMsg = (result.ErrorMessage ?? "Generation failed")
                    .Replace("\"", "'");
                await Response.WriteAsync($"data: {{\"error\":\"{errorMsg}\"}}\n\n");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SSE stream error — physician:{PhysicianId}", physicianId);
            await Response.WriteAsync("data: {\"error\":\"Internal server error\"}\n\n");
        }
        finally
        {
            await Response.WriteAsync("data: [DONE]\n\n");
            await Response.Body.FlushAsync();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/v1/test-scenarios/{id}/generate-article?index={n}
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a single Stage 2 detailed article for a test scenario.
    /// Re-runs RiskCalculatorService (Rule 1) before calling Claude API.
    /// Returns { content: string } with the generated Arabic article body.
    /// </summary>
    [HttpPost("{id:guid}/generate-article")]
    [ProducesResponseType(typeof(ApiResponse<GenerateArticleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>),                  StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>),                  StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<GenerateArticleResponse>>> GenerateArticle(
        Guid id,
        [FromQuery] int index)
    {
        var physicianId = User.FindFirst(ClaimNames.PhysicianId)?.Value;
        if (string.IsNullOrEmpty(physicianId))
            return Unauthorized(new ApiResponse<object>
            {
                Success = false, Error = "لم يتم التحقق من هوية الطبيب.", ErrorType = "MissingPhysicianClaim"
            });

        var scenario = await _db.TestScenarios
            .FirstOrDefaultAsync(s => s.ScenarioId == id && s.PhysicianId == physicianId);

        if (scenario == null)
            return NotFound(new ApiResponse<object>
            {
                Success = false, Error = $"TestScenario {id} not found.", ErrorType = "NotFound"
            });

        if (string.IsNullOrEmpty(scenario.GeneratedContentJson))
            return BadRequest(new ApiResponse<object>
            {
                Success = false, Error = "Scenario has no generated content.", ErrorType = "NoContent"
            });

        Stage1Output? stage1;
        PatientData?  patientData;
        try
        {
            stage1      = JsonSerializer.Deserialize<Stage1Output>(scenario.GeneratedContentJson, _jsonOpts);
            patientData = JsonSerializer.Deserialize<PatientData>(scenario.PatientDataJson, _jsonOpts);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialise scenario {Id}", id);
            return BadRequest(new ApiResponse<object>
            {
                Success = false, Error = "Scenario content is malformed.", ErrorType = "DeserializationError"
            });
        }

        if (stage1 == null || patientData == null)
            return BadRequest(new ApiResponse<object>
            {
                Success = false, Error = "Scenario content is malformed.", ErrorType = "DeserializationError"
            });

        if (index < 0 || index >= stage1.ArticleOutlines.Count)
            return BadRequest(new ApiResponse<object>
            {
                Success   = false,
                Error     = $"Index {index} is out of range (0–{stage1.ArticleOutlines.Count - 1}).",
                ErrorType = "IndexOutOfRange"
            });

        var articleSpec = stage1.ArticleOutlines[index];
        var riskScore   = _riskCalculator.Calculate(patientData);   // Rule 1 — always C#

        try
        {
            var result = await _apiClient.GenerateStage2Async(patientData, articleSpec, riskScore);

            if (!result.Success || result.Output == null)
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
                {
                    Success = false, Error = result.ErrorMessage ?? "Stage 2 generation failed.",
                    ErrorType = "GenerationFailed"
                });

            _logger.LogInformation(
                "GenerateArticle complete — scenario:{ScenarioId} index:{Index} article:{ArticleId}",
                id, index, articleSpec.ArticleId);

            return Ok(new ApiResponse<GenerateArticleResponse>
            {
                Success = true,
                Data    = new GenerateArticleResponse { Content = result.Output.Article.ContentAr }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "GenerateArticle error — scenario:{ScenarioId} index:{Index}", id, index);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false, Error = "Internal server error.", ErrorType = ex.GetType().Name
            });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static PatientData BuildPatientData(CreateTestScenarioRequest request) => new()
    {
        PrimaryDiagnosis    = request.PrimaryDiagnosis,
        AgeGroup            = request.AgeGroup,
        Comorbidities       = request.Comorbidities       ?? string.Empty,
        CurrentMedications  = request.CurrentMedications  ?? string.Empty,
        Allergies           = request.Allergies           ?? string.Empty,
        MedicalRestrictions = request.MedicalRestrictions ?? string.Empty
    };

    private static TestScenarioResponse MapToResponse(TestScenario scenario) => new()
    {
        ScenarioId           = scenario.ScenarioId,
        PhysicianId          = scenario.PhysicianId,
        TenantId             = scenario.TenantId,
        Status               = scenario.Status.ToString(),
        PatientDataJson      = scenario.PatientDataJson,
        GeneratedContentJson = scenario.GeneratedContentJson,
        CreatedAt            = scenario.CreatedAt,
        Evaluation           = scenario.Evaluation == null
                               ? null
                               : MapEvalToResponse(scenario.Evaluation)
    };

    private static ContentEvaluationResponse MapEvalToResponse(ContentEvaluation e) => new()
    {
        EvaluationId          = e.EvaluationId,
        ScenarioId            = e.ScenarioId,
        PhysicianId           = e.PhysicianId,
        AccuracyRating        = e.AccuracyRating,
        ClarityRating         = e.ClarityRating,
        RelevanceRating       = e.RelevanceRating,
        CompletenessRating    = e.CompletenessRating,
        IsAppropriate         = e.IsAppropriate,
        IsCulturallySensitive = e.IsCulturallySensitive,
        IsArabicQuality       = e.IsArabicQuality,
        WhatWorked            = e.WhatWorked,
        NeedsImprovement      = e.NeedsImprovement,
        Comments              = e.Comments,
        SubmittedAt           = e.SubmittedAt
    };

    private ApiResponse<object> ValidationErrorResponse() => new()
    {
        Success  = false,
        Error    = "Validation failed.",
        Metadata = new Dictionary<string, object>
        {
            ["errors"] = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList()
        }
    };
}
