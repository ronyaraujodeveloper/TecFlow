using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TecFlow.Database;

#nullable disable

namespace TecFlow.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261005070000_AddPreFlightPhase30")]
    public partial class AddPreFlightPhase30 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PreFlightNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CampaignId = table.Column<int>(type: "int", nullable: false),
                    CampaignTitle = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ProductUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CouponCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    AlertType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OriginalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CurrentPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SubstituteUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    SubstitutePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SubstituteName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreFlightNotifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PreFlightNotifications_UserId_IsResolved_CreatedAt",
                table: "PreFlightNotifications",
                columns: new[] { "UserId", "IsResolved", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PreFlightNotifications");
        }
    }
}
