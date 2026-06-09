using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TecFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLojaIdScopeToCampaignAndMetric : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LojaId",
                table: "Metricas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LojaId",
                table: "Campanhas",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Metricas_LojaId",
                table: "Metricas",
                column: "LojaId");

            migrationBuilder.CreateIndex(
                name: "IX_Campanhas_LojaId",
                table: "Campanhas",
                column: "LojaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Campanhas_IntegracaoLoja_LojaId",
                table: "Campanhas",
                column: "LojaId",
                principalTable: "IntegracaoLoja",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Metricas_IntegracaoLoja_LojaId",
                table: "Metricas",
                column: "LojaId",
                principalTable: "IntegracaoLoja",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Campanhas_IntegracaoLoja_LojaId",
                table: "Campanhas");

            migrationBuilder.DropForeignKey(
                name: "FK_Metricas_IntegracaoLoja_LojaId",
                table: "Metricas");

            migrationBuilder.DropIndex(
                name: "IX_Metricas_LojaId",
                table: "Metricas");

            migrationBuilder.DropIndex(
                name: "IX_Campanhas_LojaId",
                table: "Campanhas");

            migrationBuilder.DropColumn(
                name: "LojaId",
                table: "Metricas");

            migrationBuilder.DropColumn(
                name: "LojaId",
                table: "Campanhas");
        }
    }
}
