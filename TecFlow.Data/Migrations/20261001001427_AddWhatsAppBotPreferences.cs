using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TecFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWhatsAppBotPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableAutoConvertBot",
                table: "WhatsAppIntegrations",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReplyToGroupMessages",
                table: "WhatsAppIntegrations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ReplyToPrivateMessages",
                table: "WhatsAppIntegrations",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnableAutoConvertBot",
                table: "WhatsAppIntegrations");

            migrationBuilder.DropColumn(
                name: "ReplyToGroupMessages",
                table: "WhatsAppIntegrations");

            migrationBuilder.DropColumn(
                name: "ReplyToPrivateMessages",
                table: "WhatsAppIntegrations");
        }
    }
}
