using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatingBot.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_User_Inactivity_Tracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastActiveAt",
                table: "Users",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastInactivityReminderSentAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_LastActive_LastReminder",
                table: "Users",
                columns: new[] { "LastActiveAt", "LastInactivityReminderSentAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_LastActive_LastReminder",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastActiveAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastInactivityReminderSentAt",
                table: "Users");
        }
    }
}
