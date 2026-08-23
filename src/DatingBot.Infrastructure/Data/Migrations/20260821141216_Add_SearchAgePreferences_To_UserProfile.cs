using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatingBot.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_SearchAgePreferences_To_UserProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AgeFilters",
                table: "UserProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SearchMaxAge",
                table: "UserProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SearchMinAge",
                table: "UserProfiles",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgeFilters",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "SearchMaxAge",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "SearchMinAge",
                table: "UserProfiles");
        }
    }
}
