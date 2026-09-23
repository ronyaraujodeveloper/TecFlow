using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TecFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShortAffiliateLinkAffiliateUrlAndAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AffiliateUrl",
                table: "ShortAffiliateLinks",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MarketplaceAccountId",
                table: "ShortAffiliateLinks",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE ShortAffiliateLinks
                SET AffiliateUrl = DestinationUrl
                WHERE AffiliateUrl IS NULL OR AffiliateUrl = N'';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ShortAffiliateLinks_MarketplaceAccountId",
                table: "ShortAffiliateLinks",
                column: "MarketplaceAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShortAffiliateLinks_MarketplaceAccounts_MarketplaceAccountId",
                table: "ShortAffiliateLinks",
                column: "MarketplaceAccountId",
                principalTable: "MarketplaceAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShortAffiliateLinks_MarketplaceAccounts_MarketplaceAccountId",
                table: "ShortAffiliateLinks");

            migrationBuilder.DropIndex(
                name: "IX_ShortAffiliateLinks_MarketplaceAccountId",
                table: "ShortAffiliateLinks");

            migrationBuilder.DropColumn(name: "AffiliateUrl", table: "ShortAffiliateLinks");
            migrationBuilder.DropColumn(name: "MarketplaceAccountId", table: "ShortAffiliateLinks");
        }
    }
}
