using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TecFlow.Database;

#nullable disable

namespace TecFlow.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261005040000_AddOfferRadarMining")]
    public partial class AddOfferRadarMining : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AffiliateMiningProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    NichesCsv = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    MinTicket = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MaxTicket = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MinCommissionPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    RestrictToActiveStores = table.Column<bool>(type: "bit", nullable: false),
                    AutoPilotEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AutoPilotChannel = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AffiliateMiningProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AffiliateMiningProfiles_UserId",
                table: "AffiliateMiningProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateTable(
                name: "ProductPriceSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ProductKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    PlatformType = table.Column<int>(type: "int", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SourceUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CapturedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPriceSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductPriceSnapshots_UserId_ProductKey_CapturedAt",
                table: "ProductPriceSnapshots",
                columns: new[] { "UserId", "ProductKey", "CapturedAt" });

            migrationBuilder.CreateTable(
                name: "OfferRadarItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ProductImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OriginalUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    AffiliateUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    PlatformType = table.Column<int>(type: "int", nullable: true),
                    PlatformName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ComparedPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CouponCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    AttractivenessScore = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsAutoQueued = table.Column<bool>(type: "bit", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfferRadarItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OfferRadarItems_UserId_AttractivenessScore_ReceivedAt",
                table: "OfferRadarItems",
                columns: new[] { "UserId", "AttractivenessScore", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OfferRadarItems_UserId_OriginalUrl_ReceivedAt",
                table: "OfferRadarItems",
                columns: new[] { "UserId", "OriginalUrl", "ReceivedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "OfferRadarItems");
            migrationBuilder.DropTable(name: "ProductPriceSnapshots");
            migrationBuilder.DropTable(name: "AffiliateMiningProfiles");
        }
    }
}
