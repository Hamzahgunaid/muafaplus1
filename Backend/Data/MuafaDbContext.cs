using Microsoft.EntityFrameworkCore;
using MuafaPlus.Models;

namespace MuafaPlus.Data;

/// <summary>
/// Phase 2: PhysicianCredentials added for JWT auth.
/// Phase 1 (multi-tenant): Tenant, TenantSettings, TenantSubscription,
/// InvitationCode, UserRole, AssistantPhysicianLink added.
/// TenantId (nullable) added to Physicians for tenant scoping.
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

        // ── Seed physicians ────────────────────────────────────────────────────
        modelBuilder.Entity<Physician>().HasData(
            new Physician { PhysicianId = "PHY001", FullName = "Dr. Ahmed Al-Sana",     Specialty = "Internal Medicine", Email = "ahmed.sana@hospital.ye",  Phone = "+967-1-234567", LicenseNumber = "YE-MED-2018-001",  Institution = "Sana'a General Hospital", City = "Sana'a", Country = "Yemen", IsActive = true, CreatedAt = new DateTime(2025,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2025,1,1,0,0,0,DateTimeKind.Utc) },
            new Physician { PhysicianId = "PHY002", FullName = "Dr. Fatima Al-Hakim",   Specialty = "Cardiology",        Email = "fatima.hakim@clinic.ye",   Phone = "+967-2-345678", LicenseNumber = "YE-CARD-2019-042", Institution = "Heart Care Clinic",       City = "Aden",   Country = "Yemen", IsActive = true, CreatedAt = new DateTime(2025,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2025,1,1,0,0,0,DateTimeKind.Utc) },
            new Physician { PhysicianId = "PHY003", FullName = "Dr. Mohammed Al-Zubairi", Specialty = "Endocrinology",  Email = "mohammed.z@diabetes.ye",   Phone = "+967-1-456789", LicenseNumber = "YE-ENDO-2020-103", Institution = "Diabetes Center",          City = "Sana'a", Country = "Yemen", IsActive = true, CreatedAt = new DateTime(2025,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2025,1,1,0,0,0,DateTimeKind.Utc) }
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
