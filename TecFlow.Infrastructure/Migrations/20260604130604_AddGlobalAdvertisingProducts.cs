using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TecFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGlobalAdvertisingProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProdutosPropagandaGlobal",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GlobalProductUid = table.Column<Guid>(type: "uuid", nullable: false),
                    NomeAmigavel = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: false),
                    CategoriaGlobal = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UrlImagemPrincipal = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    PrecoMedio = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProdutosPropagandaGlobal", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProdutosPropagandaGlobal_Usuarios_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MarketplaceAffiliateLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProdutoGlobalId = table.Column<int>(type: "integer", nullable: false),
                    Marketplace = table.Column<int>(type: "integer", nullable: false),
                    UrlProdutoOriginal = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    IdProdutoPlataforma = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    LinkAfiliadoGerado = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    ParametrosRastreio = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketplaceAffiliateLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketplaceAffiliateLinks_ProdutosPropagandaGlobal_ProdutoG~",
                        column: x => x.ProdutoGlobalId,
                        principalTable: "ProdutosPropagandaGlobal",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceAffiliateLinks_ProdutoGlobalId_Marketplace",
                table: "MarketplaceAffiliateLinks",
                columns: new[] { "ProdutoGlobalId", "Marketplace" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosPropagandaGlobal_GlobalProductUid",
                table: "ProdutosPropagandaGlobal",
                column: "GlobalProductUid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosPropagandaGlobal_OwnerId",
                table: "ProdutosPropagandaGlobal",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketplaceAffiliateLinks");

            migrationBuilder.DropTable(
                name: "ProdutosPropagandaGlobal");
        }
    }
}
