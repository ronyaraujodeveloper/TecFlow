using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TecFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenantArchitecture : Migration
    {
        private const string DefaultTenantId = "a1000000-0000-4000-8000-000000000001";
        private static readonly Guid DefaultTenantGuid = new(DefaultTenantId);

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserDeviceTokens_OwnerId_Token",
                table: "UserDeviceTokens");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosPropagandaGlobal_GlobalProductUid",
                table: "ProdutosPropagandaGlobal");

            migrationBuilder.DropIndex(
                name: "IX_Produtos_SkuCodigo_MarketplaceOrigem_MarketplaceShopId",
                table: "Produtos");

            migrationBuilder.DropIndex(
                name: "IX_MarketplaceTokens_ShopId_MarketplaceType",
                table: "MarketplaceTokens");

            migrationBuilder.DropIndex(
                name: "IX_MarketplaceOrders_ExternalOrderId_MarketplaceType_ShopId",
                table: "MarketplaceOrders");

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DocumentNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.Sql($"""
                INSERT INTO "Tenants" ("Id", "Name", "IsActive", "CreatedAt")
                VALUES ('{DefaultTenantId}', 'TecFlow — Conta Padrão (migração)', TRUE, NOW() AT TIME ZONE 'UTC');
                """);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Usuarios",
                type: "uuid",
                nullable: false,
                defaultValue: DefaultTenantGuid);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "UserDeviceTokens",
                type: "uuid",
                nullable: false,
                defaultValue: DefaultTenantGuid);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ProdutosPropagandaGlobal",
                type: "uuid",
                nullable: false,
                defaultValue: DefaultTenantGuid);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Produtos",
                type: "uuid",
                nullable: false,
                defaultValue: DefaultTenantGuid);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Metricas",
                type: "uuid",
                nullable: false,
                defaultValue: DefaultTenantGuid);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "MarketplaceTokens",
                type: "uuid",
                nullable: false,
                defaultValue: DefaultTenantGuid);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "MarketplaceOrders",
                type: "uuid",
                nullable: false,
                defaultValue: DefaultTenantGuid);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "MarketplaceOrderLines",
                type: "uuid",
                nullable: false,
                defaultValue: DefaultTenantGuid);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "MarketplaceAffiliateLinks",
                type: "uuid",
                nullable: false,
                defaultValue: DefaultTenantGuid);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Conversaos",
                type: "uuid",
                nullable: false,
                defaultValue: DefaultTenantGuid);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Conteudos",
                type: "uuid",
                nullable: false,
                defaultValue: DefaultTenantGuid);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Campanhas",
                type: "uuid",
                nullable: false,
                defaultValue: DefaultTenantGuid);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Afiliados",
                type: "uuid",
                nullable: false,
                defaultValue: DefaultTenantGuid);

            migrationBuilder.CreateTable(
                name: "MarketplaceAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    MarketplaceType = table.Column<int>(type: "integer", nullable: false),
                    ShopId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ShopName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AccessToken = table.Column<string>(type: "text", nullable: false),
                    RefreshToken = table.Column<string>(type: "text", nullable: true),
                    Cnpj = table.Column<string>(type: "character varying(18)", maxLength: 18, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RefreshExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketplaceAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketplaceAccounts_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_TenantId",
                table: "Usuarios",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceTokens_TenantId_OwnerId_Token",
                table: "UserDeviceTokens",
                columns: new[] { "TenantId", "OwnerId", "Token" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosPropagandaGlobal_TenantId_GlobalProductUid",
                table: "ProdutosPropagandaGlobal",
                columns: new[] { "TenantId", "GlobalProductUid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_TenantId_SkuCodigo_MarketplaceOrigem_MarketplaceSh~",
                table: "Produtos",
                columns: new[] { "TenantId", "SkuCodigo", "MarketplaceOrigem", "MarketplaceShopId" },
                filter: "\"SkuCodigo\" IS NOT NULL AND \"MarketplaceOrigem\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceTokens_TenantId_ShopId_MarketplaceType",
                table: "MarketplaceTokens",
                columns: new[] { "TenantId", "ShopId", "MarketplaceType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceOrders_TenantId_ExternalOrderId_MarketplaceType_~",
                table: "MarketplaceOrders",
                columns: new[] { "TenantId", "ExternalOrderId", "MarketplaceType", "ShopId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceAccounts_TenantId_ShopId_MarketplaceType",
                table: "MarketplaceAccounts",
                columns: new[] { "TenantId", "ShopId", "MarketplaceType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Name",
                table: "Tenants",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Tenants_TenantId",
                table: "Usuarios",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql($"""
                INSERT INTO "MarketplaceAccounts" ("TenantId", "MarketplaceType", "ShopId", "ShopName", "AccessToken", "RefreshToken", "ExpiresAt", "RefreshExpiresAt", "CreatedAt")
                SELECT "TenantId", "MarketplaceType", "ShopId", "ShopId", "AccessToken", "RefreshToken", "ExpiresAt", "RefreshExpiresAt", "CreatedAt"
                FROM "MarketplaceTokens"
                WHERE NOT EXISTS (
                    SELECT 1 FROM "MarketplaceAccounts" ma
                    WHERE ma."TenantId" = "MarketplaceTokens"."TenantId"
                      AND ma."ShopId" = "MarketplaceTokens"."ShopId"
                      AND ma."MarketplaceType" = "MarketplaceTokens"."MarketplaceType"
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Tenants_TenantId",
                table: "Usuarios");

            migrationBuilder.DropTable(
                name: "MarketplaceAccounts");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_TenantId",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_UserDeviceTokens_TenantId_OwnerId_Token",
                table: "UserDeviceTokens");

            migrationBuilder.DropIndex(
                name: "IX_ProdutosPropagandaGlobal_TenantId_GlobalProductUid",
                table: "ProdutosPropagandaGlobal");

            migrationBuilder.DropIndex(
                name: "IX_Produtos_TenantId_SkuCodigo_MarketplaceOrigem_MarketplaceSh~",
                table: "Produtos");

            migrationBuilder.DropIndex(
                name: "IX_MarketplaceTokens_TenantId_ShopId_MarketplaceType",
                table: "MarketplaceTokens");

            migrationBuilder.DropIndex(
                name: "IX_MarketplaceOrders_TenantId_ExternalOrderId_MarketplaceType_~",
                table: "MarketplaceOrders");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "UserDeviceTokens");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ProdutosPropagandaGlobal");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Metricas");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "MarketplaceTokens");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "MarketplaceOrders");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "MarketplaceOrderLines");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "MarketplaceAffiliateLinks");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Conversaos");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Conteudos");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Campanhas");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Afiliados");

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceTokens_OwnerId_Token",
                table: "UserDeviceTokens",
                columns: new[] { "OwnerId", "Token" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosPropagandaGlobal_GlobalProductUid",
                table: "ProdutosPropagandaGlobal",
                column: "GlobalProductUid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_SkuCodigo_MarketplaceOrigem_MarketplaceShopId",
                table: "Produtos",
                columns: new[] { "SkuCodigo", "MarketplaceOrigem", "MarketplaceShopId" },
                filter: "\"SkuCodigo\" IS NOT NULL AND \"MarketplaceOrigem\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceTokens_ShopId_MarketplaceType",
                table: "MarketplaceTokens",
                columns: new[] { "ShopId", "MarketplaceType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceOrders_ExternalOrderId_MarketplaceType_ShopId",
                table: "MarketplaceOrders",
                columns: new[] { "ExternalOrderId", "MarketplaceType", "ShopId" },
                unique: true);
        }
    }
}
