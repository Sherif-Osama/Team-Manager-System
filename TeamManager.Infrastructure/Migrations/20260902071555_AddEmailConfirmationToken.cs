using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailConfirmationToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EmailConfirmationTokenExpiresAtUtc",
                table: "Users",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailConfirmationTokenHash",
                table: "Users",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailConfirmationTokenExpiresAtUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailConfirmationTokenHash",
                table: "Users");
        }
    }
}
