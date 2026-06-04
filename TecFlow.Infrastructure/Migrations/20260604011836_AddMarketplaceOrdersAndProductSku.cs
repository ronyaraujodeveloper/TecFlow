using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TecFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketplaceOrdersAndProductSku : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdExterno",
                table: "Produtos",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MarketplaceOrigem",
                table: "Produtos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MarketplaceShopId",
                table: "Produtos",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SkuCodigo",
                table: "Produtos",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MarketplaceOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExternalOrderId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ShopId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    MarketplaceType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StockDeducted = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketplaceOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketplaceOrderLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MarketplaceOrderId = table.Column<int>(type: "integer", nullable: false),
                    ExternalSkuId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SkuCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExternalProductId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketplaceOrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketplaceOrderLines_MarketplaceOrders_MarketplaceOrderId",
                        column: x => x.MarketplaceOrderId,
                        principalTable: "MarketplaceOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_SkuCodigo_MarketplaceOrigem_MarketplaceShopId",
                table: "Produtos",
                columns: new[] { "SkuCodigo", "MarketplaceOrigem", "MarketplaceShopId" },
                filter: "\"SkuCodigo\" IS NOT NULL AND \"MarketplaceOrigem\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceOrderLines_MarketplaceOrderId",
                table: "MarketplaceOrderLines",
                column: "MarketplaceOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceOrders_ExternalOrderId_MarketplaceType_ShopId",
                table: "MarketplaceOrders",
                columns: new[] { "ExternalOrderId", "MarketplaceType", "ShopId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketplaceOrderLines");

            migrationBuilder.DropTable(
                name: "MarketplaceOrders");

            migrationBuilder.DropIndex(
                name: "IX_Produtos_SkuCodigo_MarketplaceOrigem_MarketplaceShopId",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "IdExterno",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "MarketplaceOrigem",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "MarketplaceShopId",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "SkuCodigo",
                table: "Produtos");
        }
    }
}
