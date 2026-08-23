using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatingBot.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_CurrentCandidate_And_ReportDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurrentCandidateProfileId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Details",
                table: "ProfileReports",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentCandidateProfileId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Details",
                table: "ProfileReports");
        }
    }
}
