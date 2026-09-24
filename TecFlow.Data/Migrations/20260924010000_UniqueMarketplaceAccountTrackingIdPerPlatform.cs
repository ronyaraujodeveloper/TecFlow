using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TecFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class UniqueMarketplaceAccountTrackingIdPerPlatform : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceAccounts_MarketplaceType_TrackingId",
                table: "MarketplaceAccounts",
                columns: new[] { "MarketplaceType", "TrackingId" },
                unique: true,
                filter: "[IsActive] = 1 AND [TrackingId] IS NOT NULL AND [TrackingId] <> N''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MarketplaceAccounts_MarketplaceType_TrackingId",
                table: "MarketplaceAccounts");
        }
    }
}
