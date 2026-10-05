using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskAttachmentFileHashUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_TaskAttachments_SizeBytes",
                table: "TaskAttachments");

            migrationBuilder.AlterColumn<string>(
                name: "FileHash",
                table: "TaskAttachments",
                type: "char(64)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "char(64)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "UQ_TaskAttachments_TaskId_FileHash",
                table: "TaskAttachments",
                columns: new[] { "TaskId", "FileHash" },
                unique: true,
                filter: "[DeletedAtUtc] IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TaskAttachments_SizeBytes",
                table: "TaskAttachments",
                sql: "[SizeBytes] > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_TaskAttachments_TaskId_FileHash",
                table: "TaskAttachments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TaskAttachments_SizeBytes",
                table: "TaskAttachments");

            migrationBuilder.AlterColumn<string>(
                name: "FileHash",
                table: "TaskAttachments",
                type: "char(64)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "char(64)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TaskAttachments_SizeBytes",
                table: "TaskAttachments",
                sql: "[SizeBytes] >= 0");
        }
    }
}
