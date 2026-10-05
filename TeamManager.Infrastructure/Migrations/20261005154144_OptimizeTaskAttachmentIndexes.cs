using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeTaskAttachmentIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaskAttachments_TaskId",
                table: "TaskAttachments");

            migrationBuilder.CreateIndex(
                name: "IX_TaskAttachments_TaskId_UploadedAtUtc",
                table: "TaskAttachments",
                columns: new[] { "TaskId", "UploadedAtUtc" },
                filter: "[DeletedAtUtc] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaskAttachments_TaskId_UploadedAtUtc",
                table: "TaskAttachments");

            migrationBuilder.CreateIndex(
                name: "IX_TaskAttachments_TaskId",
                table: "TaskAttachments",
                column: "TaskId",
                filter: "[DeletedAtUtc] IS NULL");
        }
    }
}
