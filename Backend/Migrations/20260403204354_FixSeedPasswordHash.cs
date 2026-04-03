using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MuafaPlus.Migrations
{
    /// <inheritdoc />
    public partial class FixSeedPasswordHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "PhysicianCredentials",
                keyColumn: "PhysicianId",
                keyValue: "PHY001",
                column: "PasswordHash",
                value: "$2a$12$YA59.209V0gVCqlmF2Gu4.4ds.ETlKE/MMNT0PpTNQUQABUMbAf7i");

            migrationBuilder.UpdateData(
                table: "PhysicianCredentials",
                keyColumn: "PhysicianId",
                keyValue: "PHY002",
                column: "PasswordHash",
                value: "$2a$12$YA59.209V0gVCqlmF2Gu4.4ds.ETlKE/MMNT0PpTNQUQABUMbAf7i");

            migrationBuilder.UpdateData(
                table: "PhysicianCredentials",
                keyColumn: "PhysicianId",
                keyValue: "PHY003",
                column: "PasswordHash",
                value: "$2a$12$YA59.209V0gVCqlmF2Gu4.4ds.ETlKE/MMNT0PpTNQUQABUMbAf7i");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "PhysicianCredentials",
                keyColumn: "PhysicianId",
                keyValue: "PHY001",
                column: "PasswordHash",
                value: "$2a$12$K8BNpUn6WfEjLbhZM1Q7e.Yt5e7vSqMbXFkYpjHn1o2jNQp0vMsQK");

            migrationBuilder.UpdateData(
                table: "PhysicianCredentials",
                keyColumn: "PhysicianId",
                keyValue: "PHY002",
                column: "PasswordHash",
                value: "$2a$12$K8BNpUn6WfEjLbhZM1Q7e.Yt5e7vSqMbXFkYpjHn1o2jNQp0vMsQK");

            migrationBuilder.UpdateData(
                table: "PhysicianCredentials",
                keyColumn: "PhysicianId",
                keyValue: "PHY003",
                column: "PasswordHash",
                value: "$2a$12$K8BNpUn6WfEjLbhZM1Q7e.Yt5e7vSqMbXFkYpjHn1o2jNQp0vMsQK");
        }
    }
}
