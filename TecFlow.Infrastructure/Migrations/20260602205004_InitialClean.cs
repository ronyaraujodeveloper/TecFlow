using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TecFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialClean : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Plano = table.Column<string>(type: "text", nullable: false),
                    TikTokShopAccessToken = table.Column<string>(type: "text", nullable: true),
                    TikTokRefreshToken = table.Column<string>(type: "text", nullable: true),
                    TikTokTokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Campanhas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: false),
                    DataInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataFim = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Orcamento = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campanhas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Campanhas_Usuarios_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Conteudos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: false),
                    DataInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataFim = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Orcamento = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conteudos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Conteudos_Usuarios_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Conversaos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AfiliadoId = table.Column<int>(type: "integer", nullable: false),
                    ValorVenda = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Cliques = table.Column<int>(type: "integer", nullable: false),
                    Vendas = table.Column<int>(type: "integer", nullable: false),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversaos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Conversaos_Usuarios_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Produtos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Resumo = table.Column<string>(type: "text", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: false),
                    Caracteristicas = table.Column<string>(type: "text", nullable: false),
                    Beneficios = table.Column<string>(type: "text", nullable: false),
                    Categoria = table.Column<string>(type: "text", nullable: false),
                    PublicoAlvo = table.Column<string>(type: "text", nullable: false),
                    Preco = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataModificacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UrlImagemPrincipal = table.Column<string>(type: "text", nullable: true),
                    UrlsImagens = table.Column<List<string>>(type: "text[]", nullable: false),
                    SalesVolume = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Rating = table.Column<double>(type: "double precision", nullable: false),
                    Material = table.Column<string>(type: "text", nullable: true),
                    Estoque = table.Column<int>(type: "integer", nullable: false),
                    Cor = table.Column<string>(type: "text", nullable: true),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Produtos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Produtos_Usuarios_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Metricas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CampaignId = table.Column<int>(type: "integer", nullable: false),
                    Visualizacoes = table.Column<int>(type: "integer", nullable: false),
                    Cliques = table.Column<int>(type: "integer", nullable: false),
                    Vendas = table.Column<int>(type: "integer", nullable: false),
                    Investimento = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Receita = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    MetricaId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Metricas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Metricas_Campanhas_CampanhaId",
                        column: x => x.CampaignId,
                        principalTable: "Campanhas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Metricas_Metricas_MetricaId",
                        column: x => x.MetricaId,
                        principalTable: "Metricas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Metricas_Usuarios_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Afiliados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    CodigoAfiliado = table.Column<string>(type: "text", nullable: false),
                    Comissao = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CampaignId = table.Column<int>(type: "integer", nullable: false),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    ConteudoId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Afiliados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Afiliados_Campanhas_CampanhaId",
                        column: x => x.CampaignId,
                        principalTable: "Campanhas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Afiliados_Conteudos_ConteudoId",
                        column: x => x.ConteudoId,
                        principalTable: "Conteudos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Afiliados_Usuarios_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Afiliados_CampanhaId",
                table: "Afiliados",
                column: "CampanhaId");

            migrationBuilder.CreateIndex(
                name: "IX_Afiliados_ConteudoId",
                table: "Afiliados",
                column: "ConteudoId");

            migrationBuilder.CreateIndex(
                name: "IX_Afiliados_OwnerId",
                table: "Afiliados",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Campanhas_OwnerId",
                table: "Campanhas",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Conteudos_OwnerId",
                table: "Conteudos",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversaos_OwnerId",
                table: "Conversaos",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Metricas_CampanhaId",
                table: "Metricas",
                column: "CampanhaId");

            migrationBuilder.CreateIndex(
                name: "IX_Metricas_MetricaId",
                table: "Metricas",
                column: "MetricaId");

            migrationBuilder.CreateIndex(
                name: "IX_Metricas_OwnerId",
                table: "Metricas",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_OwnerId",
                table: "Produtos",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Afiliados");

            migrationBuilder.DropTable(
                name: "Conversaos");

            migrationBuilder.DropTable(
                name: "Metricas");

            migrationBuilder.DropTable(
                name: "Produtos");

            migrationBuilder.DropTable(
                name: "Conteudos");

            migrationBuilder.DropTable(
                name: "Campanhas");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
