using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxFailedOnUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FailedOnUtc",
                table: "OutboxMessages",
                type: "datetime2(3)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedOnUtc",
                table: "OutboxMessages");
        }
    }
}
