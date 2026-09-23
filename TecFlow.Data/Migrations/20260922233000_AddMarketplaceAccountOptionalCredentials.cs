using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TecFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketplaceAccountOptionalCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ShopId",
                table: "MarketplaceAccounts",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "FriendlyName",
                table: "MarketplaceAccounts",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "ShopName",
                table: "MarketplaceAccounts",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "AccessToken",
                table: "MarketplaceAccounts",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "MarketplaceAccounts",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AddColumn<string>(
                name: "TrackingId",
                table: "MarketplaceAccounts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AppKey",
                table: "MarketplaceAccounts",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AppSecret",
                table: "MarketplaceAccounts",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE MarketplaceAccounts
                SET TrackingId = AffiliateTrackingId
                WHERE TrackingId IS NULL AND AffiliateTrackingId IS NOT NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ShopId",
                table: "IntegracaoLoja",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "FriendlyName",
                table: "IntegracaoLoja",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "AccessToken",
                table: "IntegracaoLoja",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "TrackingId", table: "MarketplaceAccounts");
            migrationBuilder.DropColumn(name: "AppKey", table: "MarketplaceAccounts");
            migrationBuilder.DropColumn(name: "AppSecret", table: "MarketplaceAccounts");
        }
    }
}
