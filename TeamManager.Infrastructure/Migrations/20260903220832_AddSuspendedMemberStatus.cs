using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSuspendedMemberStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_TeamMembers_Status",
                table: "TeamMembers");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TeamMembers_Status",
                table: "TeamMembers",
                sql: "[Status] IN (1, 2, 3)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_TeamMembers_Status",
                table: "TeamMembers");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TeamMembers_Status",
                table: "TeamMembers",
                sql: "[Status] IN (1, 2)");
        }
    }
}
