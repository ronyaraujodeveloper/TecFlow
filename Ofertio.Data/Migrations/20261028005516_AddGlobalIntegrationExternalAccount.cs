using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ofertio.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGlobalIntegrationExternalAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalAccountId",
                table: "GlobalIntegrationTokens",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalAccountName",
                table: "GlobalIntegrationTokens",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalAccountId",
                table: "GlobalIntegrationTokens");

            migrationBuilder.DropColumn(
                name: "ExternalAccountName",
                table: "GlobalIntegrationTokens");
        }
    }
}
