using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatingBot.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_IncomingRatings_Composite_Index : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProfileRatings_ToUserId",
                table: "ProfileRatings");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileRatings_ToUser_Score_CreatedAt",
                table: "ProfileRatings",
                columns: new[] { "ToUserId", "Score", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProfileRatings_ToUser_Score_CreatedAt",
                table: "ProfileRatings");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileRatings_ToUserId",
                table: "ProfileRatings",
                column: "ToUserId");
        }
    }
}
