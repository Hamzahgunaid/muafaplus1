using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MuafaPlus.Migrations
{
    /// <inheritdoc />
    public partial class Phase2_ArticleLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArticleLibrary",
                columns: table => new
                {
                    LibraryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Stage1ResultJson = table.Column<string>(type: "text", nullable: false),
                    HitCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    FirstGeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    LastHitAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleLibrary", x => x.LibraryId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArticleLibrary_ProfileHash",
                table: "ArticleLibrary",
                column: "ProfileHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArticleLibrary_TenantId",
                table: "ArticleLibrary",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArticleLibrary");
        }
    }
}
