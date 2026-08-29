using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FilterTeamNameUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_Teams_Name",
                table: "Teams");

            migrationBuilder.CreateIndex(
                name: "UQ_Teams_Name",
                table: "Teams",
                column: "Name",
                unique: true,
                filter: "[DeletedAtUtc] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_Teams_Name",
                table: "Teams");

            migrationBuilder.CreateIndex(
                name: "UQ_Teams_Name",
                table: "Teams",
                column: "Name",
                unique: true);
        }
    }
}
