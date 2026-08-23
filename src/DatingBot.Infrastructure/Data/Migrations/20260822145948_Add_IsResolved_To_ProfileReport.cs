using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatingBot.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_IsResolved_To_ProfileReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsResolved",
                table: "ProfileReports",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAt",
                table: "ProfileReports",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfileReports_IsResolved_CreatedAt",
                table: "ProfileReports",
                columns: new[] { "IsResolved", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProfileReports_IsResolved_CreatedAt",
                table: "ProfileReports");

            migrationBuilder.DropColumn(
                name: "IsResolved",
                table: "ProfileReports");

            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                table: "ProfileReports");
        }
    }
}
