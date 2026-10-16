using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TeamManager.Infrastructure.Persistence;

#nullable disable

namespace TeamManager.Infrastructure.Migrations
{
    [DbContext(typeof(TeamManagerDbContext))]
    [Migration("20261016120000_AddOutboxProcessingLease")]
    public partial class AddOutboxProcessingLease : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LeaseId",
                table: "OutboxMessages",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LeaseExpiresAtUtc",
                table: "OutboxMessages",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Lease",
                table: "OutboxMessages",
                columns: new[] { "ProcessedOnUtc", "FailedOnUtc", "NextAttemptOnUtc", "LeaseExpiresAtUtc" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_Lease",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LeaseId",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LeaseExpiresAtUtc",
                table: "OutboxMessages");
        }
    }
}
