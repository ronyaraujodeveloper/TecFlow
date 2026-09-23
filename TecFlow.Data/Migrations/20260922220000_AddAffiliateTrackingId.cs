using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TecFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAffiliateTrackingId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AffiliateTrackingId",
                table: "MarketplaceAccounts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AffiliateTrackingId",
                table: "IntegracaoLoja",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AffiliateTrackingId",
                table: "MarketplaceAccounts");

            migrationBuilder.DropColumn(
                name: "AffiliateTrackingId",
                table: "IntegracaoLoja");
        }
    }
}
