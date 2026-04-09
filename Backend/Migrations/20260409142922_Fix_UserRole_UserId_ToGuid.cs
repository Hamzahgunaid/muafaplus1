using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MuafaPlus.Migrations
{
    /// <inheritdoc />
    public partial class Fix_UserRole_UserId_ToGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PostgreSQL cannot cast text→uuid implicitly; drop PK, convert with USING, re-add PK.
            migrationBuilder.Sql(@"
                ALTER TABLE ""UserRoles"" DROP CONSTRAINT IF EXISTS ""PK_UserRoles"";
                ALTER TABLE ""UserRoles"" ALTER COLUMN ""UserId"" TYPE uuid USING ""UserId""::uuid;
                ALTER TABLE ""UserRoles"" ADD CONSTRAINT ""PK_UserRoles"" PRIMARY KEY (""UserId"", ""TenantId"");
            ");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Users_UserId",
                table: "UserRoles",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Users_UserId",
                table: "UserRoles");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "UserRoles",
                type: "text",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }
    }
}
