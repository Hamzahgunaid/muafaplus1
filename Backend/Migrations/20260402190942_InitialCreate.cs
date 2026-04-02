using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MuafaPlus.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Physicians",
                columns: table => new
                {
                    PhysicianId = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Specialty = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LicenseNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Credentials = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Institution = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "Yemen"),
                    PreferredLanguage = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Arabic"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EmailNotifications = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SmsNotifications = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Physicians", x => x.PhysicianId);
                });

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    PatientId = table.Column<string>(type: "text", nullable: false),
                    PhysicianId = table.Column<string>(type: "text", nullable: false),
                    PrimaryDiagnosis = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    AgeGroup = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Comorbidities = table.Column<string>(type: "text", nullable: true),
                    CurrentMedications = table.Column<string>(type: "text", nullable: true),
                    Allergies = table.Column<string>(type: "text", nullable: true),
                    MedicalRestrictions = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.PatientId);
                    table.ForeignKey(
                        name: "FK_Patients_Physicians_PhysicianId",
                        column: x => x.PhysicianId,
                        principalTable: "Physicians",
                        principalColumn: "PhysicianId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhysicianCredentials",
                columns: table => new
                {
                    PhysicianId = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    MustResetOnNextLogin = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhysicianCredentials", x => x.PhysicianId);
                    table.ForeignKey(
                        name: "FK_PhysicianCredentials_Physicians_PhysicianId",
                        column: x => x.PhysicianId,
                        principalTable: "Physicians",
                        principalColumn: "PhysicianId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GenerationSessions",
                columns: table => new
                {
                    SessionId = table.Column<string>(type: "text", nullable: false),
                    PatientId = table.Column<string>(type: "text", nullable: false),
                    PhysicianId = table.Column<string>(type: "text", nullable: false),
                    Stage = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RiskLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TotalArticles = table.Column<int>(type: "integer", nullable: true),
                    TotalCost = table.Column<decimal>(type: "numeric", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GenerationSessions", x => x.SessionId);
                    table.ForeignKey(
                        name: "FK_GenerationSessions_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GenerationSessions_Physicians_PhysicianId",
                        column: x => x.PhysicianId,
                        principalTable: "Physicians",
                        principalColumn: "PhysicianId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GeneratedArticles",
                columns: table => new
                {
                    ArticleId = table.Column<string>(type: "text", nullable: false),
                    SessionId = table.Column<string>(type: "text", nullable: false),
                    ArticleType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CoverageCodes = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Content = table.Column<string>(type: "text", nullable: false),
                    WordCount = table.Column<int>(type: "integer", nullable: false),
                    TokensInput = table.Column<int>(type: "integer", nullable: false),
                    TokensOutput = table.Column<int>(type: "integer", nullable: false),
                    CostUsd = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneratedArticles", x => x.ArticleId);
                    table.ForeignKey(
                        name: "FK_GeneratedArticles_GenerationSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "GenerationSessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Physicians",
                columns: new[] { "PhysicianId", "Address", "City", "Country", "CreatedAt", "Credentials", "Department", "Email", "EmailNotifications", "FullName", "Institution", "IsActive", "LicenseNumber", "Phone", "PreferredLanguage", "Specialty", "UpdatedAt" },
                values: new object[,]
                {
                    { "PHY001", null, "Sana'a", "Yemen", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "ahmed.sana@hospital.ye", true, "Dr. Ahmed Al-Sana", "Sana'a General Hospital", true, "YE-MED-2018-001", "+967-1-234567", "Arabic", "Internal Medicine", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "PHY002", null, "Aden", "Yemen", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "fatima.hakim@clinic.ye", true, "Dr. Fatima Al-Hakim", "Heart Care Clinic", true, "YE-CARD-2019-042", "+967-2-345678", "Arabic", "Cardiology", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { "PHY003", null, "Sana'a", "Yemen", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "mohammed.z@diabetes.ye", true, "Dr. Mohammed Al-Zubairi", "Diabetes Center", true, "YE-ENDO-2020-103", "+967-1-456789", "Arabic", "Endocrinology", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "PhysicianCredentials",
                columns: new[] { "PhysicianId", "CreatedAt", "LastLoginAt", "MustResetOnNextLogin", "PasswordHash" },
                values: new object[,]
                {
                    { "PHY001", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "$2a$12$K8BNpUn6WfEjLbhZM1Q7e.Yt5e7vSqMbXFkYpjHn1o2jNQp0vMsQK" },
                    { "PHY002", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "$2a$12$K8BNpUn6WfEjLbhZM1Q7e.Yt5e7vSqMbXFkYpjHn1o2jNQp0vMsQK" },
                    { "PHY003", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "$2a$12$K8BNpUn6WfEjLbhZM1Q7e.Yt5e7vSqMbXFkYpjHn1o2jNQp0vMsQK" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedArticles_ArticleType",
                table: "GeneratedArticles",
                column: "ArticleType");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedArticles_CreatedAt",
                table: "GeneratedArticles",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedArticles_SessionId",
                table: "GeneratedArticles",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_GenerationSessions_PatientId",
                table: "GenerationSessions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_GenerationSessions_PhysicianId",
                table: "GenerationSessions",
                column: "PhysicianId");

            migrationBuilder.CreateIndex(
                name: "IX_GenerationSessions_StartedAt",
                table: "GenerationSessions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_GenerationSessions_Status",
                table: "GenerationSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_CreatedAt",
                table: "Patients",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_PhysicianId",
                table: "Patients",
                column: "PhysicianId");

            migrationBuilder.CreateIndex(
                name: "IX_Physicians_Email",
                table: "Physicians",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Physicians_IsActive",
                table: "Physicians",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Physicians_LicenseNumber",
                table: "Physicians",
                column: "LicenseNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GeneratedArticles");

            migrationBuilder.DropTable(
                name: "PhysicianCredentials");

            migrationBuilder.DropTable(
                name: "GenerationSessions");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "Physicians");
        }
    }
}
