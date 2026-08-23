using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatingBot.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_LastBotMessageId_To_User : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LastBotMessageId",
                table: "Users",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastBotMessageId",
                table: "Users");
        }
    }
}
