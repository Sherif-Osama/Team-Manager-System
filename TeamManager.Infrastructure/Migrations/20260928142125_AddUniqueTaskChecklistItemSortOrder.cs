using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueTaskChecklistItemSortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaskChecklistItems_TaskId_SortOrder",
                table: "TaskChecklistItems");

            migrationBuilder.CreateIndex(
                name: "UQ_TaskChecklistItems_TaskId_SortOrder",
                table: "TaskChecklistItems",
                columns: new[] { "TaskId", "SortOrder" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_TaskChecklistItems_TaskId_SortOrder",
                table: "TaskChecklistItems");

            migrationBuilder.CreateIndex(
                name: "IX_TaskChecklistItems_TaskId_SortOrder",
                table: "TaskChecklistItems",
                columns: new[] { "TaskId", "SortOrder" });
        }
    }
}
