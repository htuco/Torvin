using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Torvin.Services.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationSourceMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Notifications_Type",
                table: "Notifications");

            migrationBuilder.AddColumn<Guid>(
                name: "SourceMessageId",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_SourceMessageId_UserId",
                table: "Notifications",
                columns: new[] { "SourceMessageId", "UserId" },
                unique: true,
                filter: "[SourceMessageId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Notifications_Type",
                table: "Notifications",
                sql: "[Type] BETWEEN 1 AND 11");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_SourceMessageId_UserId",
                table: "Notifications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Notifications_Type",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "SourceMessageId",
                table: "Notifications");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Notifications_Type",
                table: "Notifications",
                sql: "[Type] BETWEEN 1 AND 9");
        }
    }
}
