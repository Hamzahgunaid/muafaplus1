using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MuafaPlus.Data;
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
    private readonly MuafaDbContext               _db;
    private readonly ILogger<ReferralsController> _logger;

    public ReferralsController(
        ReferralService              referrals,
        MuafaDbContext               db,
        ILogger<ReferralsController> logger)
    {
        _referrals = referrals;
        _db        = db;
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

        var tenantIdClaim = User.FindFirst("TenantId")?.Value;
        var tenantId = !string.IsNullOrEmpty(tenantIdClaim) &&
                       Guid.TryParse(tenantIdClaim, out var tid)
                       ? tid : Guid.Empty;

        if (tenantId == Guid.Empty)
            return BadRequest(new ApiResponse<object>
            {
                Success   = false,
                Error     = "Physician is not linked to a tenant. Contact your administrator.",
                ErrorType = "TenantNotLinked"
            });

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
    // GET /api/v1/referrals/{id}/chat
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets (or lazily creates) the chat thread for a referral.
    /// Accessible by the owning physician OR the patient of this referral.
    /// Returns 403 if chat is disabled at the tenant or physician level.
    /// Always includes both Arabic and English disclaimer strings.
    /// </summary>
    [HttpGet("{id:guid}/chat")]
    [ProducesResponseType(typeof(ApiResponse<ChatThreadResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>),             StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>),             StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ChatThreadResponse>>> GetChat(Guid id)
    {
        var (referral, accessError) = await LoadAndVerifyReferralAccessAsync(id);
        if (accessError != null) return accessError!;

        // ── Chat gate: tenant must have chat enabled ──────────────────────────
        var settings = await _db.TenantSettings
            .FirstOrDefaultAsync(s => s.TenantId == referral!.TenantId);

        if (settings == null || !settings.ChatEnabled)
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<object>
            {
                Success   = false,
                Error     = "Chat is not enabled for this institution.",
                ErrorType = "ChatDisabled"
            });

        // ── Find or create thread ─────────────────────────────────────────────
        var thread = await _db.ChatThreads
            .Include(t => t.Messages.OrderBy(m => m.SentAt))
            .FirstOrDefaultAsync(t => t.ReferralId == id);

        if (thread == null)
        {
            var windowDays = settings.PatientChatWindowDays > 0
                ? settings.PatientChatWindowDays : 7;

            thread = new ChatThread
            {
                ReferralId   = id,
                PhysicianId  = referral!.PhysicianId,
                IsEnabled    = true,
                ExpiresAt    = referral.CreatedAt.AddDays(windowDays),
                MessageCount = 0,
                CreatedAt    = DateTime.UtcNow
            };
            _db.ChatThreads.Add(thread);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "ChatThread {ThreadId} created — referral:{ReferralId} expires:{ExpiresAt}",
                thread.ThreadId, id, thread.ExpiresAt);
        }

        return Ok(new ApiResponse<ChatThreadResponse>
        {
            Success = true,
            Data    = MapThreadToResponse(thread)
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/v1/referrals/{id}/chat/messages
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sends a new message in the referral's chat thread.
    /// Accessible by the owning physician OR the patient.
    /// Returns 404 if thread not yet initialised (call GET /chat first).
    /// Returns 403 if chat is disabled, 410 if expired, 409 if at 10-message limit.
    /// </summary>
    [HttpPost("{id:guid}/chat/messages")]
    [ProducesResponseType(typeof(ApiResponse<ChatMessageResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>),              StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>),              StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>),              StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>),              StatusCodes.Status410Gone)]
    public async Task<ActionResult<ApiResponse<ChatMessageResponse>>> SendMessage(
        Guid id, [FromBody] SendMessageRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ValidationErrorResponse());

        var (referral, accessError) = await LoadAndVerifyReferralAccessAsync(id);
        if (accessError != null) return accessError!;

        // ── Load thread ───────────────────────────────────────────────────────
        var thread = await _db.ChatThreads
            .Include(t => t.Messages)
            .FirstOrDefaultAsync(t => t.ReferralId == id);

        if (thread == null)
            return NotFound(new ApiResponse<object>
            {
                Success   = false,
                Error     = "Chat thread not found. Call GET /referrals/{id}/chat first to initialise.",
                ErrorType = "NotFound"
            });

        // ── Gate checks ───────────────────────────────────────────────────────
        if (!thread.IsEnabled)
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<object>
            {
                Success   = false,
                Error     = "Chat is disabled for this referral.",
                ErrorType = "ChatDisabled"
            });

        if (thread.ExpiresAt <= DateTime.UtcNow)
            return StatusCode(StatusCodes.Status410Gone, new ApiResponse<object>
            {
                Success   = false,
                Error     = "Chat window has expired. The 7-day chat period for this referral has ended.",
                ErrorType = "ChatExpired"
            });

        if (thread.MessageCount >= 10)
            return Conflict(new ApiResponse<object>
            {
                Success   = false,
                Error     = "Message limit reached. This chat thread has reached the maximum of 10 messages.",
                ErrorType = "MessageLimitReached"
            });

        // ── Determine sender role ─────────────────────────────────────────────
        var role       = User.FindFirst("Role")?.Value;
        var senderRole = role == "Patient" ? SenderRole.Patient : SenderRole.Physician;

        // ── Create message ────────────────────────────────────────────────────
        var message = new ChatMessage
        {
            ThreadId   = thread.ThreadId,
            SenderRole = senderRole,
            Content    = request.Content,
            SentAt     = DateTime.UtcNow,
            IsRead     = false
        };

        _db.ChatMessages.Add(message);
        thread.MessageCount++;

        // ── Mark previous messages from the OTHER party as read ───────────────
        var otherRole = senderRole == SenderRole.Physician
            ? SenderRole.Patient : SenderRole.Physician;

        foreach (var m in thread.Messages.Where(m => m.SenderRole == otherRole && !m.IsRead))
            m.IsRead = true;

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "ChatMessage {MessageId} sent — thread:{ThreadId} role:{Role} count:{Count}",
            message.MessageId, thread.ThreadId, senderRole, thread.MessageCount);

        return Created(
            $"/api/v1/referrals/{id}/chat/messages",
            new ApiResponse<ChatMessageResponse>
            {
                Success = true,
                Data    = MapMessageToResponse(message)
            });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUT /api/v1/physicians/{physicianId}/chat-settings
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Enables or disables chat for all of this physician's referrals.
    /// Per-physician toggle — does not affect other physicians in the same tenant.
    /// Rule 4: physicianId verified against JWT claim.
    /// </summary>
    [HttpPut("~/api/v1/physicians/{physicianId}/chat-settings")]
    [ProducesResponseType(typeof(ApiResponse<bool>),   StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateChatSettings(
        string physicianId, [FromBody] UpdateChatSettingsRequest request)
    {
        // Rule 4: PhysicianId from JWT must match route parameter
        var jwtPhysicianId = User.FindFirst(ClaimNames.PhysicianId)?.Value;
        if (jwtPhysicianId != physicianId)
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<object>
            {
                Success   = false,
                Error     = "You can only update your own chat settings.",
                ErrorType = "Forbidden"
            });

        var physician = await _db.Physicians.FindAsync(physicianId);
        if (physician == null)
            return NotFound(new ApiResponse<object>
            {
                Success   = false,
                Error     = $"Physician {physicianId} not found.",
                ErrorType = "NotFound"
            });

        physician.ChatEnabled = request.ChatEnabled;
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Physician {PhysicianId} ChatEnabled → {Enabled}", physicianId, request.ChatEnabled);

        return Ok(new ApiResponse<bool> { Success = true, Data = true });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Chat helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Loads the referral and verifies the caller is the owning physician or the patient.
    /// Returns (referral, null) on success, (null, errorResult) on failure.
    /// errorResult is an ObjectResult so it can be returned from any action method.
    /// </summary>
    private async Task<(Referral? referral, ObjectResult? error)>
        LoadAndVerifyReferralAccessAsync(Guid referralId)
    {
        var role = User.FindFirst("Role")?.Value;

        var referral = await _db.Referrals
            .FirstOrDefaultAsync(r => r.ReferralId == referralId);

        if (referral == null)
            return (null, NotFound(new ApiResponse<object>
            {
                Success = false, Error = $"Referral {referralId} not found.", ErrorType = "NotFound"
            }));

        if (role == "Patient")
        {
            if (!Guid.TryParse(User.FindFirst("PatientAccessId")?.Value, out var paid)
                || referral.PatientAccessId != paid)
                return (null, NotFound(new ApiResponse<object>
                {
                    Success = false, Error = $"Referral {referralId} not found.", ErrorType = "NotFound"
                }));
        }
        else
        {
            var jwtPhysicianId = User.FindFirst(ClaimNames.PhysicianId)?.Value;
            if (referral.PhysicianId != jwtPhysicianId)
                return (null, NotFound(new ApiResponse<object>
                {
                    Success = false, Error = $"Referral {referralId} not found.", ErrorType = "NotFound"
                }));
        }

        return (referral, null);
    }

    private static ChatThreadResponse MapThreadToResponse(ChatThread thread) => new()
    {
        ThreadId     = thread.ThreadId,
        ReferralId   = thread.ReferralId,
        IsEnabled    = thread.IsEnabled,
        ExpiresAt    = thread.ExpiresAt,
        IsExpired    = thread.ExpiresAt < DateTime.UtcNow,
        MessageCount = thread.MessageCount,
        Messages     = thread.Messages
                             .OrderBy(m => m.SentAt)
                             .Select(MapMessageToResponse)
                             .ToList()
    };

    private static ChatMessageResponse MapMessageToResponse(ChatMessage m) => new()
    {
        MessageId  = m.MessageId,
        SenderRole = m.SenderRole.ToString(),
        Content    = m.Content,
        SentAt     = m.SentAt,
        IsRead     = m.IsRead
    };

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
