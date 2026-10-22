using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ofertio.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexedCatalogOffers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IndexedCatalogOffers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    SearchTerm = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ProductModel = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ProductName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ProductImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SourceUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PlatformType = table.Column<int>(type: "int", nullable: true),
                    PlatformName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Shipping = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CommissionPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    EstimatedCommission = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IndexedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndexedCatalogOffers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IndexedCatalogOffers_UserId_IndexedAt",
                table: "IndexedCatalogOffers",
                columns: new[] { "UserId", "IndexedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IndexedCatalogOffers_UserId_SearchTerm_IndexedAt",
                table: "IndexedCatalogOffers",
                columns: new[] { "UserId", "SearchTerm", "IndexedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IndexedCatalogOffers");
        }
    }
}
