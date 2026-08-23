using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatingBot.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_Search_Ratings_And_Reports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AverageRating",
                table: "UserProfiles",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "RatingCount",
                table: "UserProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ProfileRatings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfileRatings_Users_FromUserId",
                        column: x => x.FromUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProfileRatings_Users_ToUserId",
                        column: x => x.ToUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProfileReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReporterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfileReports_Users_ReportedUserId",
                        column: x => x.ReportedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProfileReports_Users_ReporterId",
                        column: x => x.ReporterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProfileRatings_FromUser_ToUser",
                table: "ProfileRatings",
                columns: new[] { "FromUserId", "ToUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfileRatings_ToUserId",
                table: "ProfileRatings",
                column: "ToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileReports_ReportedUserId",
                table: "ProfileReports",
                column: "ReportedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileReports_Reporter_Reported",
                table: "ProfileReports",
                columns: new[] { "ReporterId", "ReportedUserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProfileRatings");

            migrationBuilder.DropTable(
                name: "ProfileReports");

            migrationBuilder.DropColumn(
                name: "AverageRating",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "RatingCount",
                table: "UserProfiles");
        }
    }
}
