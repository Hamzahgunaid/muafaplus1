using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MuafaPlus.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_QualitySystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TestScenarios",
                columns: table => new
                {
                    ScenarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    PhysicianId = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientDataJson = table.Column<string>(type: "text", nullable: false),
                    GeneratedContentJson = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestScenarios", x => x.ScenarioId);
                    table.ForeignKey(
                        name: "FK_TestScenarios_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentEvaluations",
                columns: table => new
                {
                    EvaluationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScenarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    PhysicianId = table.Column<string>(type: "text", nullable: false),
                    AccuracyRating = table.Column<int>(type: "integer", nullable: false),
                    ClarityRating = table.Column<int>(type: "integer", nullable: false),
                    RelevanceRating = table.Column<int>(type: "integer", nullable: false),
                    CompletenessRating = table.Column<int>(type: "integer", nullable: false),
                    IsAppropriate = table.Column<bool>(type: "boolean", nullable: false),
                    IsCulturallySensitive = table.Column<bool>(type: "boolean", nullable: false),
                    IsArabicQuality = table.Column<bool>(type: "boolean", nullable: false),
                    WhatWorked = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    NeedsImprovement = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Comments = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentEvaluations", x => x.EvaluationId);
                    table.ForeignKey(
                        name: "FK_ContentEvaluations_TestScenarios_ScenarioId",
                        column: x => x.ScenarioId,
                        principalTable: "TestScenarios",
                        principalColumn: "ScenarioId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentEvaluations_ScenarioId",
                table: "ContentEvaluations",
                column: "ScenarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestScenarios_PhysicianId_TenantId",
                table: "TestScenarios",
                columns: new[] { "PhysicianId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_TestScenarios_TenantId",
                table: "TestScenarios",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContentEvaluations");

            migrationBuilder.DropTable(
                name: "TestScenarios");
        }
    }
}
