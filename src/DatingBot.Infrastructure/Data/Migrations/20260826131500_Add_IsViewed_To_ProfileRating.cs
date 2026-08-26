using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatingBot.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_IsViewed_To_ProfileRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsViewed",
                table: "ProfileRatings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.DropIndex(
                name: "IX_ProfileRatings_ToUser_Score_CreatedAt",
                table: "ProfileRatings");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileRatings_ToUser_IsViewed_Score_CreatedAt",
                table: "ProfileRatings",
                columns: new[] { "ToUserId", "IsViewed", "Score", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProfileRatings_ToUser_IsViewed_Score_CreatedAt",
                table: "ProfileRatings");

            migrationBuilder.DropColumn(
                name: "IsViewed",
                table: "ProfileRatings");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileRatings_ToUser_Score_CreatedAt",
                table: "ProfileRatings",
                columns: new[] { "ToUserId", "Score", "CreatedAt" });
        }
    }
}
