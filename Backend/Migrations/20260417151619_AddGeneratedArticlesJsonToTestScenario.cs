using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MuafaPlus.Migrations
{
    /// <inheritdoc />
    public partial class AddGeneratedArticlesJsonToTestScenario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GeneratedArticlesJson",
                table: "TestScenarios",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeneratedArticlesJson",
                table: "TestScenarios");
        }
    }
}
