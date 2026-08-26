using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamMembersRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TeamMembers",
                type: "rowversion",
                rowVersion: true,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TeamMembers");
        }
    }
}
