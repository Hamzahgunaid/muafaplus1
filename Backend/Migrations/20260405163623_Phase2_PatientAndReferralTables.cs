using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MuafaPlus.Migrations
{
    /// <inheritdoc />
    public partial class Phase2_PatientAndReferralTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PatientAccesses",
                columns: table => new
                {
                    AccessId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AccessCode = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientAccesses", x => x.AccessId);
                    table.ForeignKey(
                        name: "FK_PatientAccesses_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Referrals",
                columns: table => new
                {
                    ReferralId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PhysicianId = table.Column<string>(type: "text", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: false),
                    PatientAccessId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    RiskLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    WhatsAppDelivery = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ScheduledDeliveryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Referrals", x => x.ReferralId);
                    table.ForeignKey(
                        name: "FK_Referrals_PatientAccesses_PatientAccessId",
                        column: x => x.PatientAccessId,
                        principalTable: "PatientAccesses",
                        principalColumn: "AccessId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Referrals_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArticleEngagements",
                columns: table => new
                {
                    EngagementId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferralId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArticleId = table.Column<string>(type: "text", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Depth25At = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Depth50At = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Depth75At = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TimeOnArticleSeconds = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Reaction = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleEngagements", x => x.EngagementId);
                    table.ForeignKey(
                        name: "FK_ArticleEngagements_Referrals_ReferralId",
                        column: x => x.ReferralId,
                        principalTable: "Referrals",
                        principalColumn: "ReferralId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MessageLogs",
                columns: table => new
                {
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferralId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientPhone = table.Column<string>(type: "text", nullable: false),
                    MessageType = table.Column<string>(type: "text", nullable: false),
                    DeliveryStatus = table.Column<string>(type: "text", nullable: false, defaultValue: "Pending"),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageLogs", x => x.MessageId);
                    table.ForeignKey(
                        name: "FK_MessageLogs_Referrals_ReferralId",
                        column: x => x.ReferralId,
                        principalTable: "Referrals",
                        principalColumn: "ReferralId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MessageLogs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientFeedbacks",
                columns: table => new
                {
                    FeedbackId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferralId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsHelpful = table.Column<bool>(type: "boolean", nullable: false),
                    Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientFeedbacks", x => x.FeedbackId);
                    table.ForeignKey(
                        name: "FK_PatientFeedbacks_Referrals_ReferralId",
                        column: x => x.ReferralId,
                        principalTable: "Referrals",
                        principalColumn: "ReferralId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientProfiles",
                columns: table => new
                {
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferralId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrimaryDiagnosis = table.Column<string>(type: "text", nullable: false),
                    AgeGroup = table.Column<string>(type: "text", nullable: false),
                    Comorbidities = table.Column<string>(type: "text", nullable: true),
                    CurrentMedications = table.Column<string>(type: "text", nullable: true),
                    Allergies = table.Column<string>(type: "text", nullable: true),
                    MedicalRestrictions = table.Column<string>(type: "text", nullable: true),
                    ProfileHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PatientName = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientProfiles", x => x.ProfileId);
                    table.ForeignKey(
                        name: "FK_PatientProfiles_Referrals_ReferralId",
                        column: x => x.ReferralId,
                        principalTable: "Referrals",
                        principalColumn: "ReferralId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReferralEngagements",
                columns: table => new
                {
                    ReferralId = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AppOpenedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SummaryViewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Stage2RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FeedbackSubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferralEngagements", x => x.ReferralId);
                    table.ForeignKey(
                        name: "FK_ReferralEngagements_Referrals_ReferralId",
                        column: x => x.ReferralId,
                        principalTable: "Referrals",
                        principalColumn: "ReferralId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArticleEngagements_ReferralId",
                table: "ArticleEngagements",
                column: "ReferralId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageLogs_ReferralId",
                table: "MessageLogs",
                column: "ReferralId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageLogs_TenantId_CreatedAt",
                table: "MessageLogs",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientAccesses_PhoneNumber_TenantId",
                table: "PatientAccesses",
                columns: new[] { "PhoneNumber", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientAccesses_TenantId",
                table: "PatientAccesses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientFeedbacks_ReferralId",
                table: "PatientFeedbacks",
                column: "ReferralId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientProfiles_ReferralId",
                table: "PatientProfiles",
                column: "ReferralId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_PatientAccessId",
                table: "Referrals",
                column: "PatientAccessId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_TenantId_PhysicianId",
                table: "Referrals",
                columns: new[] { "TenantId", "PhysicianId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArticleEngagements");

            migrationBuilder.DropTable(
                name: "MessageLogs");

            migrationBuilder.DropTable(
                name: "PatientFeedbacks");

            migrationBuilder.DropTable(
                name: "PatientProfiles");

            migrationBuilder.DropTable(
                name: "ReferralEngagements");

            migrationBuilder.DropTable(
                name: "Referrals");

            migrationBuilder.DropTable(
                name: "PatientAccesses");
        }
    }
}
