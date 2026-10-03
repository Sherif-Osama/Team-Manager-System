using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTaskCommentSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaskComments_TaskId_CreatedAtUtc",
                table: "TaskComments");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "TaskComments");

            migrationBuilder.CreateIndex(
                name: "IX_TaskComments_TaskId_CreatedAtUtc",
                table: "TaskComments",
                columns: new[] { "TaskId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaskComments_TaskId_CreatedAtUtc",
                table: "TaskComments");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "TaskComments",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskComments_TaskId_CreatedAtUtc",
                table: "TaskComments",
                columns: new[] { "TaskId", "CreatedAtUtc" },
                filter: "[DeletedAtUtc] IS NULL");
        }
    }
}
