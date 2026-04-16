using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MuafaPlus.Migrations
{
    /// <inheritdoc />
    public partial class Phase1_SeedTestInvitationCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "InvitationCodes",
                columns: new[] { "Code", "CreatedAt", "CreatedByUserId", "ExpiresAt", "IsActive", "Role", "TenantId", "UsedAt", "UsedByUserId" },
                values: new object[] { "PH-TEST01", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", new DateTime(2027, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 2, null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "InvitationCodes",
                keyColumn: "Code",
                keyValue: "PH-TEST01");
        }
    }
}
