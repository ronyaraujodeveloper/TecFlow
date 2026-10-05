using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TecFlow.Database;

#nullable disable

namespace TecFlow.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261005050000_AddOfferIntelligencePhase29")]
    public partial class AddOfferIntelligencePhase29 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceChannel",
                table: "LinkClickLog",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceGroup",
                table: "LinkClickLog",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubId",
                table: "LinkClickLog",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OfferHealthAlerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CampaignId = table.Column<int>(type: "int", nullable: true),
                    ProductUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CouponCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    AlertType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ComparedPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfferHealthAlerts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OfferHealthAlerts_UserId_CreatedAt",
                table: "OfferHealthAlerts",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateTable(
                name: "EvergreenOffers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ProductImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AffiliateUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    OriginalUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PlatformType = table.Column<int>(type: "int", nullable: true),
                    PlatformName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ClickCount = table.Column<int>(type: "int", nullable: false),
                    ChampionScore = table.Column<int>(type: "int", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsChampion = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvergreenOffers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EvergreenOffers_UserId_ChampionScore",
                table: "EvergreenOffers",
                columns: new[] { "UserId", "ChampionScore" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "EvergreenOffers");
            migrationBuilder.DropTable(name: "OfferHealthAlerts");
            migrationBuilder.DropColumn(name: "SourceChannel", table: "LinkClickLog");
            migrationBuilder.DropColumn(name: "SourceGroup", table: "LinkClickLog");
            migrationBuilder.DropColumn(name: "SubId", table: "LinkClickLog");
        }
    }
}
