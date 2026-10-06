using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAttachmentSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskAttachments_Tasks",
                table: "TaskAttachments");

            migrationBuilder.DropIndex(
                name: "IX_TaskAttachments_TaskId_UploadedAtUtc",
                table: "TaskAttachments");

            migrationBuilder.DropIndex(
                name: "UQ_TaskAttachments_TaskId_FileHash",
                table: "TaskAttachments");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "TaskAttachments");

            migrationBuilder.CreateIndex(
                name: "IX_TaskAttachments_TaskId_UploadedAtUtc",
                table: "TaskAttachments",
                columns: new[] { "TaskId", "UploadedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UQ_TaskAttachments_TaskId_FileHash",
                table: "TaskAttachments",
                columns: new[] { "TaskId", "FileHash" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskAttachments_Tasks",
                table: "TaskAttachments",
                column: "TaskId",
                principalTable: "Tasks",
                principalColumn: "TaskId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskAttachments_Tasks",
                table: "TaskAttachments");

            migrationBuilder.DropIndex(
                name: "IX_TaskAttachments_TaskId_UploadedAtUtc",
                table: "TaskAttachments");

            migrationBuilder.DropIndex(
                name: "UQ_TaskAttachments_TaskId_FileHash",
                table: "TaskAttachments");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "TaskAttachments",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskAttachments_TaskId_UploadedAtUtc",
                table: "TaskAttachments",
                columns: new[] { "TaskId", "UploadedAtUtc" },
                filter: "[DeletedAtUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_TaskAttachments_TaskId_FileHash",
                table: "TaskAttachments",
                columns: new[] { "TaskId", "FileHash" },
                unique: true,
                filter: "[DeletedAtUtc] IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskAttachments_Tasks",
                table: "TaskAttachments",
                column: "TaskId",
                principalTable: "Tasks",
                principalColumn: "TaskId");
        }
    }
}
