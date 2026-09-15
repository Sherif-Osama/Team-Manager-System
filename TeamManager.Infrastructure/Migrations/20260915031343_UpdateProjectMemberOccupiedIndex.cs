using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProjectMemberOccupiedIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_TeamMembers_TeamId_UserId_Active",
                table: "TeamMembers");

            migrationBuilder.DropIndex(
                name: "UQ_ProjectMembers_ProjectId_UserId_Active",
                table: "ProjectMembers");

            migrationBuilder.CreateIndex(
                name: "UQ_TeamMembers_TeamId_UserId_Active",
                table: "TeamMembers",
                columns: new[] { "TeamId", "UserId" },
                unique: true,
                filter: "[Status] IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "UQ_ProjectMembers_ProjectId_UserId_Occupied",
                table: "ProjectMembers",
                columns: new[] { "ProjectId", "UserId" },
                unique: true,
                filter: "[Status] IN (1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_TeamMembers_TeamId_UserId_Active",
                table: "TeamMembers");

            migrationBuilder.DropIndex(
                name: "UQ_ProjectMembers_ProjectId_UserId_Occupied",
                table: "ProjectMembers");

            migrationBuilder.CreateIndex(
                name: "UQ_TeamMembers_TeamId_UserId_Active",
                table: "TeamMembers",
                columns: new[] { "TeamId", "UserId" },
                unique: true,
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "UQ_ProjectMembers_ProjectId_UserId_Active",
                table: "ProjectMembers",
                columns: new[] { "ProjectId", "UserId" },
                unique: true,
                filter: "[Status] = 1");
        }
    }
}
