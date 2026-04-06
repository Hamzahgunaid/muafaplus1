using Microsoft.EntityFrameworkCore;
using MuafaPlus.Models;

namespace MuafaPlus.Data;

/// <summary>
/// Phase 2: PhysicianCredentials added for JWT auth.
/// Phase 1 (multi-tenant): Tenant, TenantSettings, TenantSubscription,
/// InvitationCode, UserRole, AssistantPhysicianLink added.
/// TenantId (nullable) added to Physicians for tenant scoping.
/// Phase 2 Task 1: PatientAccess, Referral, PatientProfile, ReferralEngagement,
/// ArticleEngagement, PatientFeedback, MessageLog added.
/// </summary>
public class MuafaDbContext : DbContext
{
    public MuafaDbContext(DbContextOptions<MuafaDbContext> options)
        : base(options) { }

    // ── Phase 0 ───────────────────────────────────────────────────────────────
    public DbSet<Physician>           Physicians           { get; set; }
    public DbSet<PhysicianCredential> PhysicianCredentials { get; set; }
    public DbSet<Patient>             Patients             { get; set; }
    public DbSet<GenerationSession>   GenerationSessions   { get; set; }
    public DbSet<GeneratedArticle>    GeneratedArticles    { get; set; }

    // ── Phase 1 — multi-tenant ────────────────────────────────────────────────
    public DbSet<Tenant>                  Tenants                  { get; set; }
    public DbSet<TenantSettings>          TenantSettings           { get; set; }
    public DbSet<TenantSubscription>      TenantSubscriptions      { get; set; }
    public DbSet<InvitationCode>          InvitationCodes          { get; set; }
    public DbSet<UserRole>                UserRoles                { get; set; }
    public DbSet<AssistantPhysicianLink>  AssistantPhysicianLinks  { get; set; }

    // ── Phase 2 — referral workflow ───────────────────────────────────────────
    public DbSet<PatientAccess>       PatientAccesses      { get; set; }
    public DbSet<Referral>            Referrals            { get; set; }
    public DbSet<PatientProfile>      PatientProfiles      { get; set; }
    public DbSet<ReferralEngagement>  ReferralEngagements  { get; set; }
    public DbSet<ArticleEngagement>   ArticleEngagements   { get; set; }
    public DbSet<PatientFeedback>     PatientFeedbacks     { get; set; }
    public DbSet<MessageLog>          MessageLogs          { get; set; }

    // ── Phase 2 Task 4 — Layer 1 cost reduction ───────────────────────────────
    public DbSet<ArticleLibrary>      ArticleLibrary       { get; set; }

    // ── Phase 3 Task 1 — Quality System ──────────────────────────────────────
    public DbSet<TestScenario>        TestScenarios        { get; set; }
    public DbSet<ContentEvaluation>   ContentEvaluations   { get; set; }

    // ── Phase 3 Task 2 — Async Chat ───────────────────────────────────────────
    public DbSet<ChatThread>          ChatThreads          { get; set; }
    public DbSet<ChatMessage>         ChatMessages         { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Physician ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Physician>(entity =>
        {
            entity.HasKey(e => e.PhysicianId);
            entity.HasIndex(e => e.LicenseNumber).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.Property(e => e.Country).HasDefaultValue("Yemen");
            entity.Property(e => e.PreferredLanguage).HasDefaultValue("Arabic");
            entity.Property(e => e.EmailNotifications).HasDefaultValue(true);
            entity.Property(e => e.SmsNotifications).HasDefaultValue(false);
            entity.Property(e => e.ChatEnabled).HasDefaultValue(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
        });

        // ── PhysicianCredential ────────────────────────────────────────────────
        modelBuilder.Entity<PhysicianCredential>(entity =>
        {
            entity.HasKey(e => e.PhysicianId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.HasOne(e => e.Physician)
                  .WithOne()
                  .HasForeignKey<PhysicianCredential>(e => e.PhysicianId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Patient ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(e => e.PatientId);
            entity.HasIndex(e => e.PhysicianId);
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
            entity.HasOne(e => e.Physician)
                  .WithMany(p => p.Patients)
                  .HasForeignKey(e => e.PhysicianId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── GenerationSession ──────────────────────────────────────────────────
        modelBuilder.Entity<GenerationSession>(entity =>
        {
            entity.HasKey(e => e.SessionId);
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.PhysicianId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.StartedAt);
            entity.Property(e => e.StartedAt).HasDefaultValueSql("NOW()");
            entity.HasOne(e => e.Patient)
                  .WithMany(p => p.GenerationSessions)
                  .HasForeignKey(e => e.PatientId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Physician)
                  .WithMany(p => p.GenerationSessions)
                  .HasForeignKey(e => e.PhysicianId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── GeneratedArticle ───────────────────────────────────────────────────
        modelBuilder.Entity<GeneratedArticle>(entity =>
        {
            entity.HasKey(e => e.ArticleId);
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.ArticleType);
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.HasOne(e => e.GenerationSession)
                  .WithMany(s => s.GeneratedArticles)
                  .HasForeignKey(e => e.SessionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Physician — Phase 1: add nullable TenantId FK ─────────────────────
        modelBuilder.Entity<Physician>(entity =>
        {
            entity.HasOne<Tenant>()
                  .WithMany()
                  .HasForeignKey(e => e.TenantId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Tenant ─────────────────────────────────────────────────────────────
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.TenantId);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
        });

        // ── TenantSettings (one-to-one with Tenant) ───────────────────────────
        modelBuilder.Entity<TenantSettings>(entity =>
        {
            entity.HasKey(e => e.TenantId);
            entity.Property(e => e.NotificationDelayHours).HasDefaultValue(2);
            entity.Property(e => e.ChatEnabled).HasDefaultValue(false);
            entity.Property(e => e.PatientChatWindowDays).HasDefaultValue(7);
            entity.HasOne(e => e.Tenant)
                  .WithOne(t => t.Settings)
                  .HasForeignKey<TenantSettings>(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── TenantSubscription (one-to-many with Tenant) ──────────────────────
        modelBuilder.Entity<TenantSubscription>(entity =>
        {
            entity.HasKey(e => e.SubscriptionId);
            entity.Property(e => e.CasesUsed).HasDefaultValue(0);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasOne(e => e.Tenant)
                  .WithMany(t => t.Subscriptions)
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── InvitationCode ─────────────────────────────────────────────────────
        modelBuilder.Entity<InvitationCode>(entity =>
        {
            entity.HasKey(e => e.Code);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.HasOne(e => e.Tenant)
                  .WithMany(t => t.InvitationCodes)
                  .HasForeignKey(e => e.TenantId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── UserRole (composite PK: UserId + TenantId) ────────────────────────
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.TenantId });
            entity.HasIndex(e => new { e.UserId, e.TenantId });
            entity.HasOne(e => e.Tenant)
                  .WithMany(t => t.UserRoles)
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── AssistantPhysicianLink ─────────────────────────────────────────────
        modelBuilder.Entity<AssistantPhysicianLink>(entity =>
        {
            entity.HasKey(e => e.LinkId);
            entity.HasIndex(e => new { e.AssistantId, e.TenantId });
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasOne(e => e.Tenant)
                  .WithMany(t => t.AssistantPhysicianLinks)
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Phase 2 — PatientAccess ───────────────────────────────────────────
        modelBuilder.Entity<PatientAccess>(entity =>
        {
            entity.HasKey(e => e.AccessId);
            entity.HasIndex(e => new { e.PhoneNumber, e.TenantId }).IsUnique();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.HasOne(e => e.Tenant)
                  .WithMany(t => t.PatientAccesses)
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Phase 2 — Referral ────────────────────────────────────────────────
        modelBuilder.Entity<Referral>(entity =>
        {
            entity.HasKey(e => e.ReferralId);
            entity.HasIndex(e => new { e.TenantId, e.PhysicianId });
            entity.HasIndex(e => e.PatientAccessId);
            entity.Property(e => e.Status)
                  .HasConversion<string>();
            entity.Property(e => e.WhatsAppDelivery).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.HasOne(e => e.Tenant)
                  .WithMany(t => t.Referrals)
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.PatientAccess)
                  .WithMany(pa => pa.Referrals)
                  .HasForeignKey(e => e.PatientAccessId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Phase 2 — PatientProfile (one-to-one with Referral) ───────────────
        modelBuilder.Entity<PatientProfile>(entity =>
        {
            entity.HasKey(e => e.ProfileId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.HasOne(e => e.Referral)
                  .WithOne(r => r.Profile)
                  .HasForeignKey<PatientProfile>(e => e.ReferralId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Phase 2 — ReferralEngagement (one-to-one with Referral) ──────────
        modelBuilder.Entity<ReferralEngagement>(entity =>
        {
            entity.HasKey(e => e.ReferralId);
            entity.HasOne(e => e.Referral)
                  .WithOne(r => r.Engagement)
                  .HasForeignKey<ReferralEngagement>(e => e.ReferralId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Phase 2 — ArticleEngagement ───────────────────────────────────────
        modelBuilder.Entity<ArticleEngagement>(entity =>
        {
            entity.HasKey(e => e.EngagementId);
            entity.HasIndex(e => e.ReferralId);
            entity.Property(e => e.Reaction)
                  .HasConversion<string>();
            entity.Property(e => e.TimeOnArticleSeconds).HasDefaultValue(0);
            entity.HasOne(e => e.Referral)
                  .WithMany(r => r.ArticleEngagements)
                  .HasForeignKey(e => e.ReferralId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Phase 2 — PatientFeedback (one-to-one with Referral) ─────────────
        modelBuilder.Entity<PatientFeedback>(entity =>
        {
            entity.HasKey(e => e.FeedbackId);
            entity.HasIndex(e => e.ReferralId).IsUnique();
            entity.Property(e => e.SubmittedAt).HasDefaultValueSql("NOW()");
            entity.HasOne(e => e.Referral)
                  .WithOne(r => r.Feedback)
                  .HasForeignKey<PatientFeedback>(e => e.ReferralId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Phase 2 — MessageLog ──────────────────────────────────────────────
        modelBuilder.Entity<MessageLog>(entity =>
        {
            entity.HasKey(e => e.MessageId);
            entity.HasIndex(e => e.ReferralId);
            entity.HasIndex(e => new { e.TenantId, e.CreatedAt });
            entity.Property(e => e.MessageType)
                  .HasConversion<string>();
            entity.Property(e => e.DeliveryStatus)
                  .HasConversion<string>()
                  .HasDefaultValue(DeliveryStatus.Pending);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.HasOne(e => e.Tenant)
                  .WithMany(t => t.MessageLogs)
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Referral)
                  .WithMany(r => r.MessageLogs)
                  .HasForeignKey(e => e.ReferralId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Phase 2 Task 4 — ArticleLibrary ──────────────────────────────────
        modelBuilder.Entity<ArticleLibrary>(entity =>
        {
            entity.HasKey(e => e.LibraryId);
            entity.ToTable("ArticleLibrary");
            entity.HasIndex(e => e.ProfileHash).IsUnique();
            entity.HasIndex(e => e.TenantId);
            entity.Property(e => e.HitCount).HasDefaultValue(0);
            entity.Property(e => e.FirstGeneratedAt).HasDefaultValueSql("NOW()");
        });

        // ── Phase 3 Task 1 — TestScenario ────────────────────────────────────
        modelBuilder.Entity<TestScenario>(entity =>
        {
            entity.HasKey(e => e.ScenarioId);
            entity.HasIndex(e => new { e.PhysicianId, e.TenantId });
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.HasOne(e => e.Tenant)
                  .WithMany()
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Phase 3 Task 1 — ContentEvaluation (one-to-one with TestScenario) ─
        modelBuilder.Entity<ContentEvaluation>(entity =>
        {
            entity.HasKey(e => e.EvaluationId);
            entity.HasIndex(e => e.ScenarioId).IsUnique();
            entity.Property(e => e.SubmittedAt).HasDefaultValueSql("NOW()");
            entity.HasOne(e => e.Scenario)
                  .WithOne(s => s.Evaluation)
                  .HasForeignKey<ContentEvaluation>(e => e.ScenarioId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Phase 3 Task 2 — ChatThread (one-to-one with Referral) ───────────
        modelBuilder.Entity<ChatThread>(entity =>
        {
            entity.HasKey(e => e.ThreadId);
            entity.HasIndex(e => e.ReferralId).IsUnique();
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.MessageCount).HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.HasOne(e => e.Referral)
                  .WithOne(r => r.ChatThread)
                  .HasForeignKey<ChatThread>(e => e.ReferralId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Phase 3 Task 2 — ChatMessage (one-to-many with ChatThread) ────────
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId);
            entity.HasIndex(e => new { e.ThreadId, e.SentAt });
            entity.Property(e => e.SenderRole).HasConversion<string>();
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.SentAt).HasDefaultValueSql("NOW()");
            entity.HasOne(e => e.Thread)
                  .WithMany(t => t.Messages)
                  .HasForeignKey(e => e.ThreadId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Seed physicians ────────────────────────────────────────────────────
        modelBuilder.Entity<Physician>().HasData(
            new Physician { PhysicianId = "PHY001", FullName = "Dr. Ahmed Al-Sana",     Specialty = "Internal Medicine", Email = "ahmed.sana@hospital.ye",  Phone = "+967-1-234567", LicenseNumber = "YE-MED-2018-001",  Institution = "Sana'a General Hospital", City = "Sana'a", Country = "Yemen", IsActive = true, CreatedAt = new DateTime(2025,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2025,1,1,0,0,0,DateTimeKind.Utc) },
            new Physician { PhysicianId = "PHY002", FullName = "Dr. Fatima Al-Hakim",   Specialty = "Cardiology",        Email = "fatima.hakim@clinic.ye",   Phone = "+967-2-345678", LicenseNumber = "YE-CARD-2019-042", Institution = "Heart Care Clinic",       City = "Aden",   Country = "Yemen", IsActive = true, CreatedAt = new DateTime(2025,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2025,1,1,0,0,0,DateTimeKind.Utc) },
            new Physician { PhysicianId = "PHY003", FullName = "Dr. Mohammed Al-Zubairi", Specialty = "Endocrinology",  Email = "mohammed.z@diabetes.ye",   Phone = "+967-1-456789", LicenseNumber = "YE-ENDO-2020-103", Institution = "Diabetes Center",          City = "Sana'a", Country = "Yemen", IsActive = true, CreatedAt = new DateTime(2025,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2025,1,1,0,0,0,DateTimeKind.Utc) }
        );

        // Seed test invitation code — allows testing validate-code without generating one first
        modelBuilder.Entity<InvitationCode>().HasData(
            new InvitationCode
            {
                Code            = "PH-TEST01",
                TenantId        = null,
                Role            = TenantRole.Physician,
                CreatedByUserId = "SYSTEM",
                IsActive        = true,
                ExpiresAt       = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt       = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Seed default credentials — real BCrypt hash of "MuafaPlus2025!" at cost 12
        // Generated via BCrypt.Net.BCrypt.HashPassword("MuafaPlus2025!", workFactor: 12)
        // All accounts require password reset on first login (MustResetOnNextLogin = true)
        const string defaultHash = "$2a$12$YA59.209V0gVCqlmF2Gu4.4ds.ETlKE/MMNT0PpTNQUQABUMbAf7i";
        modelBuilder.Entity<PhysicianCredential>().HasData(
            new PhysicianCredential { PhysicianId = "PHY001", PasswordHash = defaultHash, MustResetOnNextLogin = true, CreatedAt = new DateTime(2025,1,1,0,0,0,DateTimeKind.Utc) },
            new PhysicianCredential { PhysicianId = "PHY002", PasswordHash = defaultHash, MustResetOnNextLogin = true, CreatedAt = new DateTime(2025,1,1,0,0,0,DateTimeKind.Utc) },
            new PhysicianCredential { PhysicianId = "PHY003", PasswordHash = defaultHash, MustResetOnNextLogin = true, CreatedAt = new DateTime(2025,1,1,0,0,0,DateTimeKind.Utc) }
        );
    }
}
