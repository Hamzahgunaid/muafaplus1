using Microsoft.EntityFrameworkCore;
using MuafaPlus.Data;
using MuafaPlus.Models;

namespace MuafaPlus.Services;

/// <summary>
/// Phase 1: creates and manages tenants, settings, subscriptions,
/// and assistant–physician links.
/// </summary>
public class TenantService
{
    private readonly MuafaDbContext _db;
    private readonly InvitationCodeService _invitationCodes;
    private readonly ILogger<TenantService> _logger;

    public TenantService(
        MuafaDbContext db,
        InvitationCodeService invitationCodes,
        ILogger<TenantService> logger)
    {
        _db              = db;
        _invitationCodes = invitationCodes;
        _logger          = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Create tenant
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<TenantResponse> CreateTenantAsync(CreateTenantRequest request)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        var tenantId = Guid.NewGuid();
        var now      = DateTime.UtcNow;

        // 1. Tenant record
        var tenant = new Tenant
        {
            TenantId  = tenantId,
            Name      = request.Name,
            NameAr    = request.NameAr,
            LogoUrl   = request.LogoUrl,
            IsActive  = true,
            CreatedAt = now
        };
        _db.Tenants.Add(tenant);

        // 2. Default settings
        var settings = new TenantSettings
        {
            TenantId               = tenantId,
            PatientNamePolicy      = PatientNamePolicy.ShowOptional,
            WhatsAppSenderId       = null,
            NotificationDelayHours = 2,
            ChatEnabled            = false,
            PatientChatWindowDays  = 7
        };
        _db.TenantSettings.Add(settings);

        // 3. Subscription covering the next calendar month
        var subscription = new TenantSubscription
        {
            SubscriptionId    = Guid.NewGuid(),
            TenantId          = tenantId,
            PlanType          = request.PlanType,
            CasesAllocated    = request.CasesAllocated,
            CasesUsed         = 0,
            BillingCycleStart = now,
            BillingCycleEnd   = now.AddMonths(1),
            IsActive          = true
        };
        _db.TenantSubscriptions.Add(subscription);

        // Save tenant + settings + subscription before generating the code
        // (code generation needs the tenant to exist for FK)
        await _db.SaveChangesAsync();

        // 4. HA-XXXXXX invitation code scoped to this tenant
        var codeResponse = await _invitationCodes.GenerateCodeAsync(
            new GenerateInvitationCodeRequest
            {
                TenantId     = tenantId,
                Role         = TenantRole.HospitalAdmin,
                ExpiresInDays = 30
            },
            createdByUserId: "SYSTEM");

        await tx.CommitAsync();

        _logger.LogInformation(
            "Tenant created — id:{TenantId} name:{Name} adminCode:{Code}",
            tenantId, request.Name, codeResponse.Code);

        return new TenantResponse
        {
            TenantId  = tenantId,
            Name      = tenant.Name,
            NameAr    = tenant.NameAr,
            LogoUrl   = tenant.LogoUrl,
            IsActive  = tenant.IsActive,
            CreatedAt = tenant.CreatedAt,
            Settings  = MapSettings(settings),
            ActiveSubscription = MapSubscription(subscription)
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Get one tenant
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<TenantResponse?> GetTenantAsync(Guid tenantId)
    {
        var tenant = await _db.Tenants
            .Include(t => t.Settings)
            .Include(t => t.Subscriptions)
            .FirstOrDefaultAsync(t => t.TenantId == tenantId);

        return tenant == null ? null : MapTenant(tenant);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Get all tenants
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<List<TenantResponse>> GetAllTenantsAsync()
    {
        var tenants = await _db.Tenants
            .Include(t => t.Settings)
            .Include(t => t.Subscriptions)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return tenants.Select(MapTenant).ToList();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Update tenant settings
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<TenantSettingsResponse> UpdateTenantSettingsAsync(
        Guid tenantId,
        UpdateTenantSettingsRequest request)
    {
        var settings = await _db.TenantSettings.FindAsync(tenantId)
            ?? throw new KeyNotFoundException($"No settings found for tenant {tenantId}.");

        if (request.PatientNamePolicy != null &&
            Enum.TryParse<PatientNamePolicy>(request.PatientNamePolicy, ignoreCase: true, out var policy))
            settings.PatientNamePolicy = policy;

        if (request.WhatsAppSenderId != null)
            settings.WhatsAppSenderId = request.WhatsAppSenderId == string.Empty
                ? null
                : request.WhatsAppSenderId;

        if (request.NotificationDelayHours.HasValue)
            settings.NotificationDelayHours = request.NotificationDelayHours.Value;

        if (request.WhatsAppEnabled.HasValue)
            settings.WhatsAppEnabled = request.WhatsAppEnabled.Value;

        if (request.ChatEnabled.HasValue)
            settings.ChatEnabled = request.ChatEnabled.Value;

        if (request.PatientChatWindowDays.HasValue)
            settings.PatientChatWindowDays = request.PatientChatWindowDays.Value;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Tenant settings updated — id:{TenantId}", tenantId);
        return MapSettings(settings);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Get subscription
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<TenantSubscriptionResponse?> GetTenantSubscriptionAsync(Guid tenantId)
    {
        var sub = await _db.TenantSubscriptions
            .Where(s => s.TenantId == tenantId && s.IsActive)
            .OrderByDescending(s => s.BillingCycleStart)
            .FirstOrDefaultAsync();

        return sub == null ? null : MapSubscription(sub);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Increment cases used
    // ─────────────────────────────────────────────────────────────────────────

    public async Task IncrementCasesUsedAsync(Guid tenantId)
    {
        var sub = await _db.TenantSubscriptions
            .Where(s => s.TenantId == tenantId && s.IsActive)
            .OrderByDescending(s => s.BillingCycleStart)
            .FirstOrDefaultAsync();

        if (sub == null)
        {
            _logger.LogWarning("IncrementCasesUsed: no active subscription for tenant {TenantId}", tenantId);
            return;
        }

        sub.CasesUsed++;

        if (sub.CasesUsed >= sub.CasesAllocated)
            _logger.LogWarning(
                "Tenant {TenantId} has exceeded case quota ({Used}/{Allocated})",
                tenantId, sub.CasesUsed, sub.CasesAllocated);

        await _db.SaveChangesAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Assistant–physician links
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<bool> LinkAssistantToPhysicianAsync(
        Guid tenantId,
        LinkAssistantRequest request)
    {
        var exists = await _db.AssistantPhysicianLinks.AnyAsync(l =>
            l.TenantId    == tenantId &&
            l.AssistantId == request.AssistantId &&
            l.PhysicianId == request.PhysicianId &&
            l.IsActive);

        if (exists) return false;

        _db.AssistantPhysicianLinks.Add(new AssistantPhysicianLink
        {
            LinkId      = Guid.NewGuid(),
            TenantId    = tenantId,
            AssistantId = request.AssistantId,
            PhysicianId = request.PhysicianId,
            LinkedAt    = DateTime.UtcNow,
            IsActive    = true
        });

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Assistant linked — assistant:{AssistantId} physician:{PhysicianId} tenant:{TenantId}",
            request.AssistantId, request.PhysicianId, tenantId);

        return true;
    }

    public async Task<List<string>> GetPhysiciansForAssistantAsync(
        string assistantId,
        Guid tenantId)
    {
        return await _db.AssistantPhysicianLinks
            .Where(l => l.AssistantId == assistantId &&
                        l.TenantId    == tenantId &&
                        l.IsActive)
            .Select(l => l.PhysicianId)
            .ToListAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Mapping helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static TenantResponse MapTenant(Tenant t) => new()
    {
        TenantId  = t.TenantId,
        Name      = t.Name,
        NameAr    = t.NameAr,
        LogoUrl   = t.LogoUrl,
        IsActive  = t.IsActive,
        CreatedAt = t.CreatedAt,
        Settings  = t.Settings == null ? null : MapSettings(t.Settings),
        ActiveSubscription = t.Subscriptions
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.BillingCycleStart)
            .Select(MapSubscription)
            .FirstOrDefault()
    };

    private static TenantSettingsResponse MapSettings(TenantSettings s) => new()
    {
        PatientNamePolicy      = s.PatientNamePolicy.ToString(),
        WhatsAppSenderId       = s.WhatsAppSenderId,
        WhatsAppEnabled        = s.WhatsAppEnabled,
        NotificationDelayHours = s.NotificationDelayHours,
        ChatEnabled            = s.ChatEnabled,
        PatientChatWindowDays  = s.PatientChatWindowDays
    };

    private static TenantSubscriptionResponse MapSubscription(TenantSubscription s) => new()
    {
        SubscriptionId    = s.SubscriptionId,
        PlanType          = s.PlanType,
        CasesAllocated    = s.CasesAllocated,
        CasesUsed         = s.CasesUsed,
        BillingCycleStart = s.BillingCycleStart,
        BillingCycleEnd   = s.BillingCycleEnd,
        IsActive          = s.IsActive,
        UsagePercentage   = s.CasesAllocated == 0
            ? 0m
            : Math.Round((decimal)s.CasesUsed / s.CasesAllocated * 100, 2)
    };
}
