using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TecFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class EncryptMessagingIntegrationSecrets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApiKey",
                table: "WhatsAppIntegrations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SessionData",
                table: "WhatsAppIntegrations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Token",
                table: "WhatsAppIntegrations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TelegramIntegrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApiKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SessionData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChatId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    BotUsername = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramIntegrations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramIntegrations_UserId",
                table: "TelegramIntegrations",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelegramIntegrations");

            migrationBuilder.DropColumn(
                name: "ApiKey",
                table: "WhatsAppIntegrations");

            migrationBuilder.DropColumn(
                name: "SessionData",
                table: "WhatsAppIntegrations");

            migrationBuilder.DropColumn(
                name: "Token",
                table: "WhatsAppIntegrations");
        }
    }
}
