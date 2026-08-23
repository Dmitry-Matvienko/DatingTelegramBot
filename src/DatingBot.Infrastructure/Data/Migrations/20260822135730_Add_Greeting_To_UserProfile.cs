using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatingBot.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_Greeting_To_UserProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Greeting",
                table: "UserProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Greeting",
                table: "UserProfiles");
        }
    }
}
