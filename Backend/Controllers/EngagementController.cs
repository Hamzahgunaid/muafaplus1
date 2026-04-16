using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MuafaPlus.Data;
using MuafaPlus.Models;
using MuafaPlus.Services;

namespace MuafaPlus.Controllers;

/// <summary>
/// Phase 2 Task 3: Engagement tracking endpoints.
/// All routes require a valid JWT — patients and providers both use these.
///
/// POST /api/v1/referrals/{id}/engagement   — referral-level milestones
/// POST /api/v1/articles/{articleId}/engagement — per-article scroll + reaction
/// POST /api/v1/referrals/{id}/feedback     — patient satisfaction feedback
/// </summary>
[ApiController]
[Route("api/v1")]
[Authorize]
[Produces("application/json")]
public class EngagementController : ControllerBase
{
    private readonly MuafaDbContext              _db;
    private readonly ReferralService             _referrals;
    private readonly ILogger<EngagementController> _logger;

    public EngagementController(
        MuafaDbContext               db,
        ReferralService              referrals,
        ILogger<EngagementController> logger)
    {
        _db        = db;
        _referrals = referrals;
        _logger    = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/v1/referrals/{id}/engagement
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tracks referral-level engagement milestones.
    /// Accepted EventType values: "app_opened", "summary_viewed", "stage2_requested".
    /// Timestamps are set only once (null check preserves first-seen time).
    /// </summary>
    [HttpPost("referrals/{id:guid}/engagement")]
    [ProducesResponseType(typeof(ApiResponse<bool>),   StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> TrackReferralEngagement(
        Guid id,
        [FromBody] TrackEngagementRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ValidationErrorResponse());

        var engagement = await _db.ReferralEngagements
            .FirstOrDefaultAsync(e => e.ReferralId == id);

        if (engagement == null)
        {
            // Verify the referral exists before returning 404
            var exists = await _db.Referrals.AnyAsync(r => r.ReferralId == id);
            if (!exists)
                return NotFound(new ApiResponse<object>
                {
                    Success   = false,
                    Error     = $"Referral {id} not found.",
                    ErrorType = "NotFound"
                });

            // Create engagement record on-the-fly if missing
            engagement = new ReferralEngagement { ReferralId = id };
            _db.ReferralEngagements.Add(engagement);
        }

        var now = DateTime.UtcNow;

        switch (request.EventType.ToLowerInvariant())
        {
            case "app_opened":
                engagement.AppOpenedAt ??= now;
                break;

            case "summary_viewed":
                engagement.SummaryViewedAt ??= now;
                break;

            case "stage2_requested":
                engagement.Stage2RequestedAt ??= now;
                break;

            default:
                return BadRequest(new ApiResponse<object>
                {
                    Success   = false,
                    Error     = $"Unknown event type: '{request.EventType}'. " +
                                "Accepted: app_opened, summary_viewed, stage2_requested",
                    ErrorType = "UnknownEventType"
                });
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Engagement tracked — referral:{ReferralId} event:{Event}",
            id, request.EventType);

        return Ok(new ApiResponse<bool> { Success = true, Data = true });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/v1/articles/{articleId}/engagement
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tracks per-article scroll depth, time spent, and patient reaction.
    /// Finds or creates an ArticleEngagement record for (ReferralId, ArticleId).
    ///
    /// Accepted EventType values:
    ///   "opened"  → OpenedAt
    ///   "depth_25"→ Depth25At   "depth_50"→ Depth50At   "depth_75"→ Depth75At
    ///   "completed" → CompletedAt
    ///   "like"    → Reaction = Like   "dislike" → Reaction = Dislike
    ///   "time"    → TimeOnArticleSeconds += request.TimeOnArticleSeconds
    /// </summary>
    [HttpPost("articles/{articleId}/engagement")]
    [ProducesResponseType(typeof(ApiResponse<bool>),   StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<bool>>> TrackArticleEngagement(
        string articleId,
        [FromBody] TrackArticleEngagementRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ValidationErrorResponse());

        // Find or create the ArticleEngagement for this (ReferralId, ArticleId)
        var engagement = await _db.ArticleEngagements
            .FirstOrDefaultAsync(ae => ae.ReferralId == request.ReferralId
                                    && ae.ArticleId  == articleId);

        if (engagement == null)
        {
            engagement = new ArticleEngagement
            {
                ReferralId = request.ReferralId,
                ArticleId  = articleId
            };
            _db.ArticleEngagements.Add(engagement);
        }

        var now = DateTime.UtcNow;

        switch (request.EventType.ToLowerInvariant())
        {
            case "opened":
                engagement.OpenedAt ??= now;
                break;

            case "depth_25":
                engagement.Depth25At ??= now;
                break;

            case "depth_50":
                engagement.Depth50At ??= now;
                break;

            case "depth_75":
                engagement.Depth75At ??= now;
                break;

            case "completed":
                engagement.CompletedAt ??= now;
                break;

            case "like":
                engagement.Reaction = ArticleReaction.Like;
                break;

            case "dislike":
                engagement.Reaction = ArticleReaction.Dislike;
                break;

            case "time":
                engagement.TimeOnArticleSeconds += request.TimeOnArticleSeconds;
                break;

            default:
                return BadRequest(new ApiResponse<object>
                {
                    Success   = false,
                    Error     = $"Unknown event type: '{request.EventType}'. " +
                                "Accepted: opened, depth_25, depth_50, depth_75, " +
                                "completed, like, dislike, time",
                    ErrorType = "UnknownEventType"
                });
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Article engagement tracked — referral:{ReferralId} article:{ArticleId} event:{Event}",
            request.ReferralId, articleId, request.EventType);

        return Ok(new ApiResponse<bool> { Success = true, Data = true });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/v1/referrals/{id}/feedback
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Submits patient satisfaction feedback for a referral (one per referral).
    /// Moved here from ReferralsController in Phase 2 Task 3.
    /// Returns 409 Conflict if feedback already submitted.
    /// </summary>
    [HttpPost("referrals/{id:guid}/feedback")]
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
