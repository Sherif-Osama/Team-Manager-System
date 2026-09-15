using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeMyProjectsQuery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectMembers_UserId_Status",
                table: "ProjectMembers");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_UserId_Status",
                table: "ProjectMembers",
                columns: new[] { "UserId", "Status", "AddedAtUtc" })
                .Annotation("SqlServer:Include", new[] { "ProjectId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectMembers_UserId_Status",
                table: "ProjectMembers");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_UserId_Status",
                table: "ProjectMembers",
                columns: new[] { "UserId", "Status" })
                .Annotation("SqlServer:Include", new[] { "ProjectId" });
        }
    }
}
