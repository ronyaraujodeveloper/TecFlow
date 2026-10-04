using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TecFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramUserBotCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserBotApiHash",
                table: "TelegramIntegrations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserBotApiId",
                table: "TelegramIntegrations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserBotPhone",
                table: "TelegramIntegrations",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserBotApiHash",
                table: "TelegramIntegrations");

            migrationBuilder.DropColumn(
                name: "UserBotApiId",
                table: "TelegramIntegrations");

            migrationBuilder.DropColumn(
                name: "UserBotPhone",
                table: "TelegramIntegrations");
        }
    }
}
