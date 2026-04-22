using Hangfire;
using Microsoft.EntityFrameworkCore;
using MuafaPlus.Data;
using MuafaPlus.Models;

namespace MuafaPlus.Services;

/// <summary>
/// Phase 2: Orchestrates referral creation, WhatsApp delivery scheduling,
/// and engagement tracking.
/// DeliverReferralAsync and TriggerPatientStage2JobAsync are public Hangfire jobs.
/// </summary>
public class ReferralService
{
    private readonly MuafaDbContext           _db;
    private readonly WhatsAppService          _whatsApp;
    private readonly WorkflowService          _workflow;
    private readonly ProfileHashService       _profileHash;
    private readonly ILogger<ReferralService> _logger;

    public ReferralService(
        MuafaDbContext            db,
        WhatsAppService           whatsApp,
        WorkflowService           workflow,
        ProfileHashService        profileHash,
        ILogger<ReferralService>  logger)
    {
        _db          = db;
        _whatsApp    = whatsApp;
        _workflow    = workflow;
        _profileHash = profileHash;
        _logger      = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CreateReferralAsync — called by ReferralsController
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a complete referral record in 8 steps:
    /// 1. Validate tenant + subscription
    /// 2. Upsert PatientAccess (generate 4-digit code)
    /// 3. Create PatientProfile (SHA-256 hash included)
    /// 4. Create Referral record
    /// 5. Create ReferralEngagement record (all timestamps null)
    /// 6. Generate Stage 1 via WorkflowService + link SessionId (sync; Layer 1 cache)
    /// 7. Schedule Hangfire delivery job
    /// 8. Return ReferralResponse with Stage1Complete status
    /// </summary>
    public async Task<ReferralResponse> CreateReferralAsync(
        CreateReferralRequest request,
        string physicianId,
        Guid   tenantId)
    {
        // ── Step 1: Validate tenant + subscription ────────────────────────────
        var subscription = await _db.TenantSubscriptions
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.IsActive);

        if (subscription == null)
            throw new InvalidOperationException("Tenant has no active subscription");

        var settings = await _db.TenantSettings
            .FirstOrDefaultAsync(s => s.TenantId == tenantId);

        int delayHours = request.NotificationDelayHours
                         ?? settings?.NotificationDelayHours
                         ?? 2;

        // ── Step 2: Upsert PatientAccess ──────────────────────────────────────
        var code = Random.Shared.Next(1000, 9999).ToString();

        var access = await _db.PatientAccesses
            .FirstOrDefaultAsync(a => a.PhoneNumber == request.PatientPhone
                                   && a.TenantId    == tenantId);

        if (access != null)
        {
            access.AccessCode = code;
            access.IsActive   = true;
        }
        else
        {
            access = new PatientAccess
            {
                TenantId    = tenantId,
                PhoneNumber = request.PatientPhone,
                AccessCode  = code,
                IsActive    = true,
                CreatedAt   = DateTime.UtcNow
            };
            _db.PatientAccesses.Add(access);
        }

        await _db.SaveChangesAsync();

        // ── Step 3: Create PatientProfile ─────────────────────────────────────
        // Phase 2 Task 4: compute SHA-256 hash at creation time for ArticleLibrary lookups.
        var profileHash = _profileHash.GenerateHash(request);

        var profile = new PatientProfile
        {
            ReferralId          = Guid.Empty,   // set after referral is saved below
            PrimaryDiagnosis    = request.PrimaryDiagnosis,
            AgeGroup            = request.AgeGroup,
            Comorbidities       = request.Comorbidities,
            CurrentMedications  = request.CurrentMedications,
            Allergies           = request.Allergies,
            MedicalRestrictions = request.MedicalRestrictions,
            PatientName         = request.PatientName,
            ProfileHash         = profileHash,
            CreatedAt           = DateTime.UtcNow
        };

        // ── Step 4: Create Referral ───────────────────────────────────────────
        var scheduledAt = DateTime.UtcNow.AddHours(delayHours);

        var referral = new Referral
        {
            TenantId            = tenantId,
            PhysicianId         = physicianId,   // always from JWT — Rule 4
            CreatedByUserId     = physicianId,
            PatientAccessId     = access.AccessId,
            SessionId           = null,          // linked in Phase 2 Task 3
            Status              = ReferralStatus.Created,
            WhatsAppDelivery    = request.WhatsAppDelivery,
            ScheduledDeliveryAt = scheduledAt,
            CreatedAt           = DateTime.UtcNow
        };

        _db.Referrals.Add(referral);
        await _db.SaveChangesAsync();

        // Now we have the ReferralId — assign it to profile
        profile.ReferralId = referral.ReferralId;
        _db.PatientProfiles.Add(profile);

        // ── Step 5: Create ReferralEngagement (all timestamps null) ───────────
        var engagement = new ReferralEngagement
        {
            ReferralId = referral.ReferralId
        };
        _db.ReferralEngagements.Add(engagement);

        await _db.SaveChangesAsync();

        // ── Step 6: Generate Stage 1 and link SessionId ───────────────────────
        // WorkflowService runs Stage 1 synchronously (5-15s), then enqueues
        // Stage 2 as a Hangfire job. The SessionId is stored on the Referral so
        // DeliverReferralAsync can load the summary article for WhatsApp delivery.
        // ArticleLibrary (Layer 1) serves cache hits at $0 cost.
        var patientData = new PatientData
        {
            PrimaryDiagnosis    = request.PrimaryDiagnosis,
            AgeGroup            = request.AgeGroup,
            Comorbidities       = request.Comorbidities       ?? string.Empty,
            CurrentMedications  = request.CurrentMedications  ?? string.Empty,
            Allergies           = request.Allergies           ?? string.Empty,
            MedicalRestrictions = request.MedicalRestrictions ?? string.Empty
        };

        var workflowResult = await _workflow.ExecuteCompleteWorkflowAsync(physicianId, patientData);

        if (workflowResult.Success)
        {
            referral.SessionId = workflowResult.SessionId;
            referral.RiskLevel = workflowResult.RiskScore?.RiskLevelString;
            referral.Status    = ReferralStatus.Stage1Complete;
            await _db.SaveChangesAsync();
            _logger.LogInformation(
                "Referral {ReferralId} Stage 1 linked — session:{SessionId} risk:{Risk}",
                referral.ReferralId, workflowResult.SessionId,
                workflowResult.RiskScore?.RiskLevelString ?? "UNKNOWN");
        }
        else
        {
            _logger.LogWarning(
                "Referral {ReferralId} Stage 1 failed — WhatsApp delivery will be skipped — {Error}",
                referral.ReferralId, workflowResult.ErrorMessage);
        }

        // ── Step 7: Schedule Hangfire delivery job ────────────────────────────
        var delay = scheduledAt - DateTime.UtcNow;

        if (delay <= TimeSpan.Zero)
            delay = TimeSpan.FromSeconds(5);   // immediate if delay already passed

        BackgroundJob.Schedule<ReferralService>(
            svc => svc.DeliverReferralAsync(referral.ReferralId),
            delay);

        _logger.LogInformation(
            "Referral {ReferralId} created — patient:{Phone} delivery in {Hours}h",
            referral.ReferralId, request.PatientPhone, delayHours);

        // ── Step 8: Return ReferralResponse ──────────────────────────────────
        return MapToResponse(referral, access.PhoneNumber, engagement);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DeliverReferralAsync — Hangfire background job
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Delivers WhatsApp messages for the referral.
    /// Called by Hangfire on schedule. Must be public for Hangfire serialisation.
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task DeliverReferralAsync(Guid referralId)
    {
        _logger.LogInformation("DeliverReferral starting — {ReferralId}", referralId);

        // Step 1: Load referral with all required navigations
        var referral = await _db.Referrals
            .Include(r => r.PatientAccess)
            .Include(r => r.Profile)
            .Include(r => r.Engagement)
            .Include(r => r.Tenant)
                .ThenInclude(t => t!.Settings)
            .FirstOrDefaultAsync(r => r.ReferralId == referralId);

        if (referral == null)
        {
            _logger.LogWarning("DeliverReferral: referral {ReferralId} not found", referralId);
            return;
        }

        // Step 2: Stage 1 must be complete before delivery
        if (string.IsNullOrEmpty(referral.SessionId))
        {
            _logger.LogWarning(
                "DeliverReferral: referral {ReferralId} has no SessionId — Stage 1 not linked yet",
                referralId);
            return;
        }

        // Step 3: Load summary text from GenerationSession
        var summaryArticle = await _db.GeneratedArticles
            .Where(a => a.SessionId == referral.SessionId && a.ArticleType == "summary")
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();

        var summaryText = summaryArticle?.Content ?? string.Empty;
        var phone       = referral.PatientAccess?.PhoneNumber ?? string.Empty;
        var accessCode  = referral.PatientAccess?.AccessCode  ?? string.Empty;
        var physician   = referral.PhysicianId;
        var riskLevel   = referral.RiskLevel ?? "Unknown";
        var now         = DateTime.UtcNow;

        if (referral.WhatsAppDelivery)
        {
            // Step 4a: Send summary article (Message 1)
            var (ok1, err1) = await _whatsApp.SendSummaryMessageAsync(
                phone, summaryText, physician, riskLevel);

            await LogMessageAsync(
                referral, MessageType.WhatsAppSummary, phone,
                ok1 ? DeliveryStatus.Sent : DeliveryStatus.Failed, err1, now);

            // Small gap between messages
            Thread.Sleep(2000);

            // Step 4b: Send access code (Message 2)
            var patientName = referral.Profile?.PatientName ?? string.Empty;

            var (ok2, err2) = await _whatsApp.SendAccessCodeAsync(
                phone, accessCode, patientName);

            await LogMessageAsync(
                referral, MessageType.WhatsAppCode, phone,
                ok2 ? DeliveryStatus.Sent : DeliveryStatus.Failed, err2, now);
        }
        else
        {
            // Step 5: SMS fallback
            var (okSms, errSms) = await _whatsApp.SendSmsAsync(phone, accessCode, physician);

            await LogMessageAsync(
                referral, MessageType.SMS, phone,
                okSms ? DeliveryStatus.Sent : DeliveryStatus.Failed, errSms, now);
        }

        // Step 6: Update Referral status
        referral.Status      = ReferralStatus.Stage1Delivered;
        referral.DeliveredAt = now;

        // Step 7: Update engagement milestone
        if (referral.Engagement != null)
            referral.Engagement.MessageSentAt = now;
        else
            _db.ReferralEngagements.Add(new ReferralEngagement
            {
                ReferralId     = referralId,
                MessageSentAt  = now
            });

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "DeliverReferral complete — {ReferralId} status:{Status}",
            referralId, referral.Status);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Query helpers (used by ReferralsController)
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<List<ReferralResponse>> GetReferralsForPhysicianAsync(string physicianId)
    {
        var referrals = await _db.Referrals
            .Include(r => r.PatientAccess)
            .Include(r => r.Engagement)
            .Where(r => r.PhysicianId == physicianId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return referrals
            .Select(r => MapToResponse(r, r.PatientAccess?.PhoneNumber ?? string.Empty, r.Engagement))
            .ToList();
    }

    public async Task<ReferralResponse?> GetReferralAsync(Guid referralId, string physicianId)
    {
        var referral = await _db.Referrals
            .Include(r => r.PatientAccess)
            .Include(r => r.Engagement)
            .Include(r => r.ChatThread)
            .FirstOrDefaultAsync(r => r.ReferralId == referralId
                                   && r.PhysicianId == physicianId);

        return referral == null
            ? null
            : MapToResponse(referral, referral.PatientAccess?.PhoneNumber ?? string.Empty,
                            referral.Engagement);
    }

    public async Task<(bool success, string? error)> SubmitFeedbackAsync(
        Guid referralId, PatientFeedbackRequest request)
    {
        var referral = await _db.Referrals
            .Include(r => r.Feedback)
            .Include(r => r.Engagement)
            .FirstOrDefaultAsync(r => r.ReferralId == referralId);

        if (referral == null)
            return (false, "Referral not found");

        if (referral.Feedback != null)
            return (false, "Conflict");

        var now = DateTime.UtcNow;

        _db.PatientFeedbacks.Add(new PatientFeedback
        {
            ReferralId  = referralId,
            IsHelpful   = request.IsHelpful,
            Comment     = request.Comment,
            SubmittedAt = now
        });

        referral.Status = ReferralStatus.FeedbackSubmitted;

        if (referral.Engagement != null)
            referral.Engagement.FeedbackSubmittedAt = now;

        await _db.SaveChangesAsync();
        return (true, null);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Phase 2 Task 3 — Patient-facing queries
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<List<ReferralResponse>> GetReferralsForPatientAsync(Guid patientAccessId)
    {
        var referrals = await _db.Referrals
            .Include(r => r.PatientAccess)
            .Include(r => r.Engagement)
            .Where(r => r.PatientAccessId == patientAccessId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return referrals
            .Select(r => MapToResponse(r, r.PatientAccess?.PhoneNumber ?? string.Empty, r.Engagement))
            .ToList();
    }

    /// <summary>
    /// Returns articles for a referral. Verifies the caller owns the referral either
    /// as the patient (patientAccessId) or the physician (physicianId).
    /// Also sets SummaryViewedAt on first load.
    /// </summary>
    public async Task<(List<ReferralArticleResponse>? Articles, string? Error)>
        GetArticlesForReferralAsync(Guid referralId, Guid? patientAccessId, string? physicianId)
    {
        var referral = await _db.Referrals
            .Include(r => r.Engagement)
            .FirstOrDefaultAsync(r => r.ReferralId == referralId);

        if (referral == null)
            return (null, "Referral not found");

        // Verify ownership
        var isPatient   = patientAccessId.HasValue && referral.PatientAccessId == patientAccessId;
        var isPhysician = !string.IsNullOrEmpty(physicianId)  && referral.PhysicianId == physicianId;

        if (!isPatient && !isPhysician)
            return (null, "Referral not found");

        if (string.IsNullOrEmpty(referral.SessionId))
        {
            return ([], null);   // empty list; caller checks Metadata for the message
        }

        var articles = await _db.GeneratedArticles
            .Where(a => a.SessionId == referral.SessionId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();

        // Set SummaryViewedAt on first article load by patient
        if (isPatient && referral.Engagement != null && referral.Engagement.SummaryViewedAt == null)
        {
            referral.Engagement.SummaryViewedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        var result = articles.Select(a => new ReferralArticleResponse
        {
            ArticleId    = a.ArticleId,
            ArticleType  = a.ArticleType,
            ContentAr    = a.Content,
            CoverageCodes = a.CoverageCodes,
            WordCount    = a.WordCount,
            CreatedAt    = a.CreatedAt
        }).ToList();

        return (result, null);
    }

    /// <summary>
    /// Validates referral status and queues patient-triggered Stage 2.
    /// Returns (true, null) on success, (false, errorMessage) on validation failure.
    /// </summary>
    public async Task<(bool success, string? error, int statusCode)> RequestPatientStage2Async(
        Guid referralId, Guid patientAccessId)
    {
        var referral = await _db.Referrals
            .Include(r => r.Engagement)
            .FirstOrDefaultAsync(r => r.ReferralId    == referralId
                                   && r.PatientAccessId == patientAccessId);

        if (referral == null)
            return (false, "Referral not found", 404);

        if (referral.Status != ReferralStatus.Stage1Complete &&
            referral.Status != ReferralStatus.Stage1Delivered)
            return (false, "Stage 1 must complete before requesting Stage 2", 400);

        if (referral.Engagement?.Stage2RequestedAt != null)
            return (false, "Stage 2 already requested", 409);

        var now = DateTime.UtcNow;

        referral.Status = ReferralStatus.Stage2Requested;

        if (referral.Engagement != null)
            referral.Engagement.Stage2RequestedAt = now;
        else
            _db.ReferralEngagements.Add(new ReferralEngagement
            {
                ReferralId       = referralId,
                Stage2RequestedAt = now
            });

        await _db.SaveChangesAsync();

        // Queue Hangfire job — returns immediately (Rule 3)
        BackgroundJob.Enqueue<ReferralService>(
            svc => svc.TriggerPatientStage2JobAsync(referralId));

        _logger.LogInformation(
            "Stage 2 requested by patient — referral:{ReferralId}", referralId);

        return (true, null, 202);
    }

    /// <summary>
    /// Hangfire job: runs the complete workflow (Stage 1 + Stage 2) using patient
    /// profile data and links the new session back to the referral.
    /// Stage 1 is re-run to obtain article outlines needed for Stage 2.
    /// Phase 2 Task 9 (ArticleLibrary) will serve Stage 1 from cache, eliminating
    /// the duplicate API call.
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task TriggerPatientStage2JobAsync(Guid referralId)
    {
        _logger.LogInformation("TriggerPatientStage2 starting — referral:{ReferralId}", referralId);

        var referral = await _db.Referrals
            .Include(r => r.Profile)
            .FirstOrDefaultAsync(r => r.ReferralId == referralId);

        if (referral == null || referral.Profile == null)
        {
            _logger.LogWarning("TriggerPatientStage2: referral or profile not found — {ReferralId}", referralId);
            return;
        }

        var patientData = new PatientData
        {
            PrimaryDiagnosis    = referral.Profile.PrimaryDiagnosis,
            AgeGroup            = referral.Profile.AgeGroup,
            Comorbidities       = referral.Profile.Comorbidities       ?? string.Empty,
            CurrentMedications  = referral.Profile.CurrentMedications  ?? string.Empty,
            Allergies           = referral.Profile.Allergies           ?? string.Empty,
            MedicalRestrictions = referral.Profile.MedicalRestrictions ?? string.Empty
        };

        // Runs Stage 1 (for article outlines) then immediately enqueues Stage 2 jobs
        var result = await _workflow.ExecuteCompleteWorkflowAsync(
            referral.PhysicianId, patientData);

        if (result.Success)
        {
            referral.SessionId = result.SessionId;
            referral.Status    = ReferralStatus.Stage2Complete;
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "TriggerPatientStage2 complete — referral:{ReferralId} session:{SessionId}",
                referralId, result.SessionId);
        }
        else
        {
            _logger.LogError(
                "TriggerPatientStage2 failed — referral:{ReferralId} error:{Error}",
                referralId, result.ErrorMessage);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Phase 2 Task 3 — Provider engagement detail view
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<ReferralEngagementDetailResponse?> GetEngagementDetailAsync(
        Guid referralId, string physicianId)
    {
        var referral = await _db.Referrals
            .Include(r => r.PatientAccess)
            .Include(r => r.Engagement)
            .Include(r => r.ArticleEngagements)
            .Include(r => r.Feedback)
            .FirstOrDefaultAsync(r => r.ReferralId == referralId
                                   && r.PhysicianId == physicianId);

        if (referral == null)
            return null;

        return new ReferralEngagementDetailResponse
        {
            ReferralId   = referral.ReferralId,
            Status       = referral.Status.ToString(),
            RiskLevel    = referral.RiskLevel,
            PatientPhone = referral.PatientAccess?.PhoneNumber ?? string.Empty,

            Timeline = referral.Engagement == null ? null : new ReferralEngagementResponse
            {
                MessageSentAt       = referral.Engagement.MessageSentAt,
                AppOpenedAt         = referral.Engagement.AppOpenedAt,
                SummaryViewedAt     = referral.Engagement.SummaryViewedAt,
                Stage2RequestedAt   = referral.Engagement.Stage2RequestedAt,
                FeedbackSubmittedAt = referral.Engagement.FeedbackSubmittedAt
            },

            Articles = referral.ArticleEngagements.Select(ae => new ArticleEngagementResponse
            {
                ArticleId            = ae.ArticleId,
                OpenedAt             = ae.OpenedAt,
                Depth25At            = ae.Depth25At,
                Depth50At            = ae.Depth50At,
                Depth75At            = ae.Depth75At,
                CompletedAt          = ae.CompletedAt,
                TimeOnArticleSeconds = ae.TimeOnArticleSeconds,
                Reaction             = ae.Reaction.ToString()
            }).ToList(),

            Feedback = referral.Feedback == null ? null : new PatientFeedbackResponse
            {
                IsHelpful   = referral.Feedback.IsHelpful,
                Comment     = referral.Feedback.Comment,
                SubmittedAt = referral.Feedback.SubmittedAt
            }
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────────

    private async Task LogMessageAsync(
        Referral       referral,
        MessageType    type,
        string         phone,
        DeliveryStatus status,
        string?        error,
        DateTime       sentAt)
    {
        _db.MessageLogs.Add(new MessageLog
        {
            TenantId       = referral.TenantId,
            ReferralId     = referral.ReferralId,
            RecipientPhone = phone,
            MessageType    = type,
            DeliveryStatus = status,
            SentAt         = sentAt,
            DeliveredAt    = status == DeliveryStatus.Sent ? sentAt : null,
            ErrorMessage   = error,
            CreatedAt      = sentAt
        });

        await _db.SaveChangesAsync();
    }

    private static ReferralResponse MapToResponse(
        Referral            referral,
        string              phone,
        ReferralEngagement? engagement)
    {
        return new ReferralResponse
        {
            ReferralId          = referral.ReferralId,
            Status              = referral.Status.ToString(),
            RiskLevel           = referral.RiskLevel,
            PatientPhone        = phone,
            WhatsAppDelivery    = referral.WhatsAppDelivery,
            ScheduledDeliveryAt = referral.ScheduledDeliveryAt,
            DeliveredAt         = referral.DeliveredAt,
            CreatedAt           = referral.CreatedAt,
            SessionId           = referral.SessionId,
            ChatEnabled         = referral.ChatEnabled,
            Engagement          = engagement == null ? null : new ReferralEngagementResponse
            {
                MessageSentAt       = engagement.MessageSentAt,
                AppOpenedAt         = engagement.AppOpenedAt,
                SummaryViewedAt     = engagement.SummaryViewedAt,
                Stage2RequestedAt   = engagement.Stage2RequestedAt,
                FeedbackSubmittedAt = engagement.FeedbackSubmittedAt
            }
        };
    }
}
