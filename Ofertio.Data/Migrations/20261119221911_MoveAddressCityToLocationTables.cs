using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ofertio.Data.Migrations
{
    /// <summary>
    /// Endereço do perfil (PerfisCadastrais) e do cliente de venda (Customers) passa a guardar só o CityId.
    /// Conversão: casa cidade + UF com a tabela de referência, sem diferenciar acento nem maiúsculas;
    /// o cadastro que tinha cidade ou UF preenchida e não casou fica em São Paulo/SP (IBGE 3550308).
    /// Cadastro sem nenhum endereço continua sem cidade.
    /// </summary>
    public partial class MoveAddressCityToLocationTables : Migration
    {
        private const string SaoPauloIbgeCode = "3550308";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "PerfisCadastrais",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "Customers",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(ConvertTextToCityId("PerfisCadastrais"));
            migrationBuilder.Sql(ConvertTextToCityId("Customers"));

            migrationBuilder.DropColumn(
                name: "City",
                table: "PerfisCadastrais");

            migrationBuilder.DropColumn(
                name: "State",
                table: "PerfisCadastrais");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Customers");

            migrationBuilder.CreateIndex(
                name: "IX_PerfisCadastrais_CityId",
                table: "PerfisCadastrais",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CityId",
                table: "Customers",
                column: "CityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Cities_CityId",
                table: "Customers",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PerfisCadastrais_Cities_CityId",
                table: "PerfisCadastrais",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Cities_CityId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_PerfisCadastrais_Cities_CityId",
                table: "PerfisCadastrais");

            migrationBuilder.DropIndex(
                name: "IX_PerfisCadastrais_CityId",
                table: "PerfisCadastrais");

            migrationBuilder.DropIndex(
                name: "IX_Customers_CityId",
                table: "Customers");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "PerfisCadastrais",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "PerfisCadastrais",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Customers",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Customers",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            // Volta o texto a partir da cidade gravada.
            foreach (var table in new[] { "PerfisCadastrais", "Customers" })
            {
                migrationBuilder.Sql(
                    $"UPDATE t SET t.[City] = c.[Name], t.[State] = s.[Code] " +
                    $"FROM [{table}] AS t INNER JOIN [Cities] AS c ON c.[Id] = t.[CityId] INNER JOIN [States] AS s ON s.[Id] = c.[StateId];");
            }

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "PerfisCadastrais");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "Customers");
        }

        /// <summary>
        /// Executado via EXEC(N'...'): no script idempotente (dotnet ef migrations script --idempotent) o SQL Server
        /// compila o bloco antes de avaliar o IF NOT EXISTS; sem o EXEC, rodar o script de novo depois que City/State
        /// já foram removidas falharia com "Invalid column name".
        /// </summary>
        private static string ConvertTextToCityId(string table) =>
            $"EXEC(N'{ConvertTextToCityIdSql(table).Replace("'", "''")}');";

        private static string ConvertTextToCityIdSql(string table) =>
            $"""
            UPDATE t SET t.[CityId] = COALESCE(
                (SELECT TOP (1) c.[Id]
                   FROM [Cities] AS c
                   INNER JOIN [States] AS s ON s.[Id] = c.[StateId]
                  WHERE s.[Code] = UPPER(LTRIM(RTRIM(t.[State])))
                    AND c.[Name] = LTRIM(RTRIM(t.[City])) COLLATE Latin1_General_CI_AI),
                (SELECT TOP (1) c.[Id] FROM [Cities] AS c WHERE c.[IbgeCode] = {SaoPauloIbgeCode}))
            FROM [{table}] AS t
            WHERE NULLIF(LTRIM(RTRIM(t.[City])), '') IS NOT NULL
               OR NULLIF(LTRIM(RTRIM(t.[State])), '') IS NOT NULL;
            """;
    }
}
