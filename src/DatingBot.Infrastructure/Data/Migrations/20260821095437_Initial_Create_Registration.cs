using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DatingBot.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial_Create_Registration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Interests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Interests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TelegramId = table.Column<long>(type: "bigint", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    State = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Gender = table.Column<int>(type: "int", nullable: true),
                    TargetGender = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Age = table.Column<int>(type: "int", nullable: true),
                    City = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Height = table.Column<int>(type: "int", nullable: true),
                    PhotoFileId = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DatingTarget = table.Column<int>(type: "int", nullable: true),
                    AiDescription = table.Column<string>(type: "nvarchar(2500)", maxLength: 2500, nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserProfileInterests",
                columns: table => new
                {
                    UserProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InterestId = table.Column<int>(type: "int", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfileInterests", x => new { x.UserProfileId, x.InterestId });
                    table.ForeignKey(
                        name: "FK_UserProfileInterests_Interests_InterestId",
                        column: x => x.InterestId,
                        principalTable: "Interests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserProfileInterests_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Interests",
                columns: new[] { "Id", "Code", "Icon", "Title" },
                values: new object[,]
                {
                    { 1, 1, "🎵", "Музыка" },
                    { 2, 2, "🎮", "Видеоигры" },
                    { 3, 3, "🎬", "Фильмы" },
                    { 4, 4, "⛩️", "Аниме" },
                    { 5, 5, "⚽", "Спорт" },
                    { 6, 6, "📚", "Книги" },
                    { 7, 7, "✈️", "Путешествия" },
                    { 8, 8, "🎨", "Искусство" },
                    { 9, 9, "🍳", "Кулинария" },
                    { 10, 10, "💻", "Технологии" },
                    { 11, 11, "🎲", "Настолки" },
                    { 12, 12, "🏕️", "Активный отдых" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Interests_Code",
                table: "Interests",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfileInterests_InterestId",
                table: "UserProfileInterests",
                column: "InterestId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_City",
                table: "UserProfiles",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_Matchmaking_Filter",
                table: "UserProfiles",
                columns: new[] { "IsCompleted", "Gender", "TargetGender", "DatingTarget" });

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_UserId",
                table: "UserProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_TelegramId",
                table: "Users",
                column: "TelegramId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserProfileInterests");

            migrationBuilder.DropTable(
                name: "Interests");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
