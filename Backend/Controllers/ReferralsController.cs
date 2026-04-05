using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MuafaPlus.Models;
using MuafaPlus.Services;

namespace MuafaPlus.Controllers;

/// <summary>
/// Phase 2: Referral management endpoints.
/// All routes require a valid JWT (Rule 6).
/// Patient JWT uses "Role"="Patient" and "PatientAccessId" claims.
/// Provider JWT uses ClaimTypes.NameIdentifier for PhysicianId.
/// Feedback endpoint moved to EngagementController (Phase 2 Task 3).
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
    /// Returns 202 Accepted.
    ///
    /// TODO Phase 2 Task 3: After creating referral, trigger Stage 1 generation
    /// and link the SessionId back to this referral.
    /// For now, referral is created without Stage 1 — existing generate endpoint
    /// still works independently.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ReferralResponse>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiResponse<object>),           StatusCodes.Status400BadRequest)]
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
    /// Lists referrals. Behaviour depends on role:
    ///   Patient  → returns referrals for this PatientAccessId (from JWT)
    ///   Provider → returns referrals owned by this PhysicianId (existing behaviour)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ReferralResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<ReferralResponse>>>> GetAll()
    {
        var role      = User.FindFirst("Role")?.Value;
        bool isPatient = role == "Patient";

        if (isPatient)
        {
            var patientAccessId = Guid.TryParse(
                User.FindFirst("PatientAccessId")?.Value, out var paid) ? paid : Guid.Empty;

            var referrals = await _referrals.GetReferralsForPatientAsync(patientAccessId);
            return Ok(new ApiResponse<List<ReferralResponse>> { Success = true, Data = referrals });
        }

        var physicianId = User.FindFirst(ClaimNames.PhysicianId)?.Value;
        if (string.IsNullOrEmpty(physicianId))
            return Unauthorized(new ApiResponse<object>
            {
                Success   = false,
                Error     = "لم يتم التحقق من هوية الطبيب.",
                ErrorType = "MissingPhysicianClaim"
            });

        var providerReferrals = await _referrals.GetReferralsForPhysicianAsync(physicianId);
        return Ok(new ApiResponse<List<ReferralResponse>> { Success = true, Data = providerReferrals });
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
    // GET /api/v1/referrals/{id}/articles
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns available articles for a referral progressively.
    /// Accessible by the patient (PatientAccessId matches) or the owning physician.
    /// Sets SummaryViewedAt on first patient load.
    /// If SessionId is not yet linked, returns an empty list with a metadata message.
    /// </summary>
    [HttpGet("{id:guid}/articles")]
    [ProducesResponseType(typeof(ApiResponse<List<ReferralArticleResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>),                        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<List<ReferralArticleResponse>>>> GetArticles(Guid id)
    {
        var role = User.FindFirst("Role")?.Value;

        Guid?   patientAccessId = null;
        string? physicianId     = null;

        if (role == "Patient")
        {
            if (Guid.TryParse(User.FindFirst("PatientAccessId")?.Value, out var paid))
                patientAccessId = paid;
        }
        else
        {
            physicianId = User.FindFirst(ClaimNames.PhysicianId)?.Value;
        }

        var (articles, error) = await _referrals.GetArticlesForReferralAsync(
            id, patientAccessId, physicianId);

        if (error == "Referral not found")
            return NotFound(new ApiResponse<object>
            {
                Success   = false,
                Error     = $"Referral {id} not found.",
                ErrorType = "NotFound"
            });

        var metadata = articles?.Count == 0
            ? new Dictionary<string, object> { ["message"] = "Content not yet generated" }
            : null;

        return Ok(new ApiResponse<List<ReferralArticleResponse>>
        {
            Success  = true,
            Data     = articles ?? [],
            Metadata = metadata
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/v1/referrals/{id}/stage2
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Patient triggers Stage 2 generation on demand.
    /// Requires Role=Patient JWT claim — providers cannot trigger patient Stage 2.
    /// Returns 202 Accepted immediately; generation runs in Hangfire (Rule 3).
    /// </summary>
    [HttpPost("{id:guid}/stage2")]
    [ProducesResponseType(typeof(ApiResponse<bool>),   StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<bool>>> TriggerStage2(Guid id)
    {
        var role = User.FindFirst("Role")?.Value;
        if (role != "Patient")
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<object>
            {
                Success   = false,
                Error     = "This endpoint is for patients only.",
                ErrorType = "Forbidden"
            });

        if (!Guid.TryParse(User.FindFirst("PatientAccessId")?.Value, out var patientAccessId))
            return Unauthorized(new ApiResponse<object>
            {
                Success   = false,
                Error     = "Invalid patient token.",
                ErrorType = "InvalidToken"
            });

        var (success, error, statusCode) =
            await _referrals.RequestPatientStage2Async(id, patientAccessId);

        if (!success)
            return statusCode switch
            {
                404 => NotFound(new ApiResponse<object>
                    { Success = false, Error = error, ErrorType = "NotFound" }),
                409 => Conflict(new ApiResponse<object>
                    { Success = false, Error = error, ErrorType = "Conflict" }),
                _   => BadRequest(new ApiResponse<object>
                    { Success = false, Error = error, ErrorType = "BusinessRuleViolation" })
            };

        return Accepted(new ApiResponse<bool> { Success = true, Data = true });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/v1/referrals/{id}/engagement
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the complete engagement timeline for a referral.
    /// Provider (physician) role only — patients use GET /referrals instead.
    /// Returns 404 if not found or not owned by the current physician.
    /// </summary>
    [HttpGet("{id:guid}/engagement")]
    [ProducesResponseType(typeof(ApiResponse<ReferralEngagementDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>),                           StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ReferralEngagementDetailResponse>>> GetEngagementDetail(
        Guid id)
    {
        var physicianId = User.FindFirst(ClaimNames.PhysicianId)?.Value;
        if (string.IsNullOrEmpty(physicianId))
            return Unauthorized(new ApiResponse<object>
            {
                Success   = false,
                Error     = "لم يتم التحقق من هوية الطبيب.",
                ErrorType = "MissingPhysicianClaim"
            });

        var detail = await _referrals.GetEngagementDetailAsync(id, physicianId);
        if (detail == null)
            return NotFound(new ApiResponse<object>
            {
                Success   = false,
                Error     = $"Referral {id} not found.",
                ErrorType = "NotFound"
            });

        return Ok(new ApiResponse<ReferralEngagementDetailResponse> { Success = true, Data = detail });
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
