using Microsoft.EntityFrameworkCore;
using MuafaPlus.Models;

namespace MuafaPlus.Data;

/// <summary>
/// Phase 2: PhysicianCredentials added for JWT auth.
/// DeleteBehavior aligned with SQL schema throughout.
/// Seed data included for physicians and default credentials.
/// </summary>
public class MuafaDbContext : DbContext
{
    public MuafaDbContext(DbContextOptions<MuafaDbContext> options)
        : base(options) { }

    public DbSet<Physician>           Physicians           { get; set; }
    public DbSet<PhysicianCredential> PhysicianCredentials { get; set; }
    public DbSet<Patient>             Patients             { get; set; }
    public DbSet<GenerationSession>   GenerationSessions   { get; set; }
    public DbSet<GeneratedArticle>    GeneratedArticles    { get; set; }

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

        // ── Seed physicians ────────────────────────────────────────────────────
        modelBuilder.Entity<Physician>().HasData(
            new Physician { PhysicianId = "PHY001", FullName = "Dr. Ahmed Al-Sana",     Specialty = "Internal Medicine", Email = "ahmed.sana@hospital.ye",  Phone = "+967-1-234567", LicenseNumber = "YE-MED-2018-001",  Institution = "Sana'a General Hospital", City = "Sana'a", Country = "Yemen", IsActive = true, CreatedAt = new DateTime(2025,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2025,1,1,0,0,0,DateTimeKind.Utc) },
            new Physician { PhysicianId = "PHY002", FullName = "Dr. Fatima Al-Hakim",   Specialty = "Cardiology",        Email = "fatima.hakim@clinic.ye",   Phone = "+967-2-345678", LicenseNumber = "YE-CARD-2019-042", Institution = "Heart Care Clinic",       City = "Aden",   Country = "Yemen", IsActive = true, CreatedAt = new DateTime(2025,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2025,1,1,0,0,0,DateTimeKind.Utc) },
            new Physician { PhysicianId = "PHY003", FullName = "Dr. Mohammed Al-Zubairi", Specialty = "Endocrinology",  Email = "mohammed.z@diabetes.ye",   Phone = "+967-1-456789", LicenseNumber = "YE-ENDO-2020-103", Institution = "Diabetes Center",          City = "Sana'a", Country = "Yemen", IsActive = true, CreatedAt = new DateTime(2025,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2025,1,1,0,0,0,DateTimeKind.Utc) }
        );

        // Seed default credentials — bcrypt hash of "MuafaPlus2025!" with cost 12
        // All accounts require password reset on first login (MustResetOnNextLogin = true)
        const string defaultHash = "$2a$12$K8BNpUn6WfEjLbhZM1Q7e.Yt5e7vSqMbXFkYpjHn1o2jNQp0vMsQK";
        modelBuilder.Entity<PhysicianCredential>().HasData(
            new PhysicianCredential { PhysicianId = "PHY001", PasswordHash = defaultHash, MustResetOnNextLogin = true, CreatedAt = new DateTime(2025,1,1,0,0,0,DateTimeKind.Utc) },
            new PhysicianCredential { PhysicianId = "PHY002", PasswordHash = defaultHash, MustResetOnNextLogin = true, CreatedAt = new DateTime(2025,1,1,0,0,0,DateTimeKind.Utc) },
            new PhysicianCredential { PhysicianId = "PHY003", PasswordHash = defaultHash, MustResetOnNextLogin = true, CreatedAt = new DateTime(2025,1,1,0,0,0,DateTimeKind.Utc) }
        );
    }
}
