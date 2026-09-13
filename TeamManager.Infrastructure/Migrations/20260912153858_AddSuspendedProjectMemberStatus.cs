using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSuspendedProjectMemberStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ProjectMembers_Status",
                table: "ProjectMembers");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProjectMembers_Status",
                table: "ProjectMembers",
                sql: "[Status] IN (1, 2, 3)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ProjectMembers_Status",
                table: "ProjectMembers");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProjectMembers_Status",
                table: "ProjectMembers",
                sql: "[Status] IN (1, 2)");
        }
    }
}
