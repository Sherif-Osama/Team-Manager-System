using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRowVersionAndRemovedByToProjectMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RemovedBy",
                table: "ProjectMembers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ProjectMembers",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_RemovedBy",
                table: "ProjectMembers",
                column: "RemovedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectMembers_RemovedBy",
                table: "ProjectMembers",
                column: "RemovedBy",
                principalTable: "Users",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectMembers_RemovedBy",
                table: "ProjectMembers");

            migrationBuilder.DropIndex(
                name: "IX_ProjectMembers_RemovedBy",
                table: "ProjectMembers");

            migrationBuilder.DropColumn(
                name: "RemovedBy",
                table: "ProjectMembers");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ProjectMembers");
        }
    }
}
