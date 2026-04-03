using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MuafaPlus.Models;
using MuafaPlus.Services;

namespace MuafaPlus.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ContentGenerationController : ControllerBase
{
    private readonly WorkflowService _workflow;
    private readonly ILogger<ContentGenerationController> _logger;

    public ContentGenerationController(WorkflowService workflow, ILogger<ContentGenerationController> logger)
    {
        _workflow = workflow;
        _logger   = logger;
    }

    /// <summary>
    /// Phase 3: Returns immediately after Stage 1 (~5-15s). Stage 2 runs in background.
    /// Poll GET /api/v1/Session/{sessionId}/status until "complete",
    /// then GET /api/v1/Session/{sessionId} for full results.
    /// </summary>
    [HttpPost("generate/complete")]
    [EnableRateLimiting("GenerationsPerHour")]
    [ProducesResponseType(typeof(ApiResponse<AsyncWorkflowResult>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ApiResponse<AsyncWorkflowResult>>> GenerateComplete(
        [FromBody] GenerateContentRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ValidationErrorResponse());

        var physicianId = User.FindFirst(ClaimNames.PhysicianId)?.Value;
        if (string.IsNullOrEmpty(physicianId))
            return BadRequest(new ApiResponse<object> { Success = false, Error = "لم يتم التحقق من هوية الطبيب.", ErrorType = "MissingPhysicianClaim" });

        try
        {
            var result = await _workflow.ExecuteCompleteWorkflowAsync(physicianId, request.PatientData);

            if (result.Success)
                return Accepted(new ApiResponse<AsyncWorkflowResult>
                {
                    Success = true, Data = result,
                    Metadata = new Dictionary<string, object>
                    {
                        ["session_id"]    = result.SessionId,
                        ["risk_level"]    = result.RiskScore?.RiskLevelString ?? "UNKNOWN",
                        ["article_count"] = result.ArticleCount,
                        ["status"]        = "stage_2_queued",
                        ["poll_url"]      = $"/api/v1/Session/{result.SessionId}/status"
                    }
                });

            return StatusCode(500, new ApiResponse<object> { Success = false, Error = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GenerateComplete error");
            return StatusCode(500, new ApiResponse<object>
            { Success = false, Error = "Internal server error", ErrorType = ex.GetType().Name });
        }
    }

    [HttpPost("generate/stage1")]
    [ProducesResponseType(typeof(ApiResponse<Stage1Result>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Stage1Result>>> GenerateStage1(
        [FromBody] GenerateContentRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ValidationErrorResponse());
        var physicianId = User.FindFirst(ClaimNames.PhysicianId)?.Value;
        if (string.IsNullOrEmpty(physicianId))
            return BadRequest(new ApiResponse<object> { Success = false, Error = "لم يتم التحقق من هوية الطبيب.", ErrorType = "MissingPhysicianClaim" });

        try
        {
            var result = await _workflow.ExecuteStage1OnlyAsync(physicianId, request.PatientData);

            return result.Success
                ? Ok(new ApiResponse<Stage1Result> { Success = true, Data = result })
                : StatusCode(500, new ApiResponse<object> { Success = false, Error = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<object>
            { Success = false, Error = "Internal server error", ErrorType = ex.GetType().Name });
        }
    }

    [HttpGet("health")]
    [AllowAnonymous]
    public ActionResult<object> Health() => Ok(new { status = "healthy", timestamp = DateTime.UtcNow, version = "1.3.0" });

    private ApiResponse<object> ValidationErrorResponse() => new()
    {
        Success = false, Error = "Validation failed.",
        Metadata = new Dictionary<string, object>
        {
            ["errors"] = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
        }
    };
}
