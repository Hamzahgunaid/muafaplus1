using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MuafaPlus.Models;
using MuafaPlus.Services;

namespace MuafaPlus.Controllers;

/// <summary>
/// Phase 2: Referral management endpoints.
/// All routes require a valid JWT (Rule 6) except the feedback endpoint,
/// which is AllowAnonymous until patient auth is implemented (Phase 2 Task 3).
/// </summary>
[ApiController]
[Route("api/v1/referrals")]
[Authorize]
[Produces("application/json")]
public class ReferralsController : ControllerBase
{
    private readonly ReferralService              _referrals;
    private readonly ILogger<ReferralsController> _logger;

    public ReferralsController(
        ReferralService              referrals,
        ILogger<ReferralsController> logger)
    {
        _referrals = referrals;
        _logger    = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/v1/referrals
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new referral and schedules WhatsApp delivery.
    /// Returns 202 Accepted — Stage 1 generation is triggered separately
    /// and linked back to this referral in Phase 2 Task 3.
    ///
    /// TODO Phase 2 Task 3: After creating referral, trigger Stage 1 generation
    /// and link the SessionId back to this referral.
    /// For now, referral is created without Stage 1 — existing generate endpoint
    /// still works independently.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ReferralResponse>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiResponse<object>),           StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>),           StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ReferralResponse>>> Create(
        [FromBody] CreateReferralRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ValidationErrorResponse());

        var physicianId = User.FindFirst(ClaimNames.PhysicianId)?.Value;
        if (string.IsNullOrEmpty(physicianId))
            return Unauthorized(new ApiResponse<object>
            {
                Success   = false,
                Error     = "لم يتم التحقق من هوية الطبيب.",
                ErrorType = "MissingPhysicianClaim"
            });

        // TenantId from JWT when tenant-aware login is introduced in Phase 3.
        // For now physicians are not yet scoped to a single tenant, so use Empty.
        var tenantId = Guid.TryParse(
            User.FindFirst("TenantId")?.Value, out var tid) ? tid : Guid.Empty;

        try
        {
            var referral = await _referrals.CreateReferralAsync(request, physicianId, tenantId);

            return Accepted(new ApiResponse<ReferralResponse> { Success = true, Data = referral });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Create referral failed — {Message}", ex.Message);
            return BadRequest(new ApiResponse<object>
            {
                Success   = false,
                Error     = ex.Message,
                ErrorType = "BusinessRuleViolation"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create referral unexpected error");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success   = false,
                Error     = "Internal server error",
                ErrorType = ex.GetType().Name
            });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/v1/referrals
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lists referrals for the authenticated physician, newest first.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ReferralResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<ReferralResponse>>>> GetAll()
    {
        var physicianId = User.FindFirst(ClaimNames.PhysicianId)?.Value;
        if (string.IsNullOrEmpty(physicianId))
            return Unauthorized(new ApiResponse<object>
            {
                Success   = false,
                Error     = "لم يتم التحقق من هوية الطبيب.",
                ErrorType = "MissingPhysicianClaim"
            });

        var referrals = await _referrals.GetReferralsForPhysicianAsync(physicianId);

        return Ok(new ApiResponse<List<ReferralResponse>> { Success = true, Data = referrals });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/v1/referrals/{id}
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a single referral with full engagement timeline.
    /// Returns 404 if not found or not owned by the authenticated physician.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ReferralResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>),           StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ReferralResponse>>> GetById(Guid id)
    {
        var physicianId = User.FindFirst(ClaimNames.PhysicianId)?.Value;
        if (string.IsNullOrEmpty(physicianId))
            return Unauthorized(new ApiResponse<object>
            {
                Success   = false,
                Error     = "لم يتم التحقق من هوية الطبيب.",
                ErrorType = "MissingPhysicianClaim"
            });

        var referral = await _referrals.GetReferralAsync(id, physicianId);
        if (referral == null)
            return NotFound(new ApiResponse<object>
            {
                Success   = false,
                Error     = $"Referral {id} not found.",
                ErrorType = "NotFound"
            });

        return Ok(new ApiResponse<ReferralResponse> { Success = true, Data = referral });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/v1/referrals/{id}/feedback
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Submits patient feedback for a referral.
    /// AllowAnonymous — patient JWT auth implemented in Phase 2 Task 3.
    /// Returns 409 Conflict if feedback has already been submitted.
    /// </summary>
    [HttpPost("{id:guid}/feedback")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<bool>),   StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<bool>>> SubmitFeedback(
        Guid id,
        [FromBody] PatientFeedbackRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ValidationErrorResponse());

        var (success, error) = await _referrals.SubmitFeedbackAsync(id, request);

        if (!success && error == "Referral not found")
            return NotFound(new ApiResponse<object>
            {
                Success   = false,
                Error     = $"Referral {id} not found.",
                ErrorType = "NotFound"
            });

        if (!success && error == "Conflict")
            return Conflict(new ApiResponse<object>
            {
                Success   = false,
                Error     = "Feedback has already been submitted for this referral.",
                ErrorType = "Conflict"
            });

        return Ok(new ApiResponse<bool> { Success = true, Data = true });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private ApiResponse<object> ValidationErrorResponse() => new()
    {
        Success = false,
        Error   = "Validation failed.",
        Metadata = new Dictionary<string, object>
        {
            ["errors"] = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList()
        }
    };
}
