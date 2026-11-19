using Microsoft.EntityFrameworkCore.Migrations;
using Ofertio.Data.Seed;

#nullable disable

namespace Ofertio.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "PublicPageEvents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CountryId",
                table: "PublicPageEvents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StateId",
                table: "PublicPageEvents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "LinkClickLog",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CountryId",
                table: "LinkClickLog",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StateId",
                table: "LinkClickLog",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsoCode = table.Column<string>(type: "char(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "States",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CountryId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "char(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IbgeCode = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_States", x => x.Id);
                    table.ForeignKey(
                        name: "FK_States_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IbgeCode = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cities_States_StateId",
                        column: x => x.StateId,
                        principalTable: "States",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PublicPageEvents_CityId",
                table: "PublicPageEvents",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicPageEvents_CountryId",
                table: "PublicPageEvents",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicPageEvents_StateId",
                table: "PublicPageEvents",
                column: "StateId");

            migrationBuilder.CreateIndex(
                name: "IX_LinkClickLog_CityId",
                table: "LinkClickLog",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_LinkClickLog_CountryId",
                table: "LinkClickLog",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_LinkClickLog_StateId",
                table: "LinkClickLog",
                column: "StateId");

            migrationBuilder.CreateIndex(
                name: "IX_Cities_IbgeCode",
                table: "Cities",
                column: "IbgeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cities_StateId_NormalizedName",
                table: "Cities",
                columns: new[] { "StateId", "NormalizedName" });

            migrationBuilder.CreateIndex(
                name: "IX_Countries_IsoCode",
                table: "Countries",
                column: "IsoCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_States_CountryId_Code",
                table: "States",
                columns: new[] { "CountryId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_States_IbgeCode",
                table: "States",
                column: "IbgeCode",
                unique: true);

            // Carga das tabelas de referência: países (ISO), estados e municípios do IBGE.
            foreach (var statement in LocationSeed.InsertStatements())
            {
                migrationBuilder.Sql(statement);
            }

            migrationBuilder.AddForeignKey(
                name: "FK_LinkClickLog_Cities_CityId",
                table: "LinkClickLog",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LinkClickLog_Countries_CountryId",
                table: "LinkClickLog",
                column: "CountryId",
                principalTable: "Countries",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LinkClickLog_States_StateId",
                table: "LinkClickLog",
                column: "StateId",
                principalTable: "States",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PublicPageEvents_Cities_CityId",
                table: "PublicPageEvents",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PublicPageEvents_Countries_CountryId",
                table: "PublicPageEvents",
                column: "CountryId",
                principalTable: "Countries",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PublicPageEvents_States_StateId",
                table: "PublicPageEvents",
                column: "StateId",
                principalTable: "States",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LinkClickLog_Cities_CityId",
                table: "LinkClickLog");

            migrationBuilder.DropForeignKey(
                name: "FK_LinkClickLog_Countries_CountryId",
                table: "LinkClickLog");

            migrationBuilder.DropForeignKey(
                name: "FK_LinkClickLog_States_StateId",
                table: "LinkClickLog");

            migrationBuilder.DropForeignKey(
                name: "FK_PublicPageEvents_Cities_CityId",
                table: "PublicPageEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_PublicPageEvents_Countries_CountryId",
                table: "PublicPageEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_PublicPageEvents_States_StateId",
                table: "PublicPageEvents");

            migrationBuilder.DropTable(
                name: "Cities");

            migrationBuilder.DropTable(
                name: "States");

            migrationBuilder.DropTable(
                name: "Countries");

            migrationBuilder.DropIndex(
                name: "IX_PublicPageEvents_CityId",
                table: "PublicPageEvents");

            migrationBuilder.DropIndex(
                name: "IX_PublicPageEvents_CountryId",
                table: "PublicPageEvents");

            migrationBuilder.DropIndex(
                name: "IX_PublicPageEvents_StateId",
                table: "PublicPageEvents");

            migrationBuilder.DropIndex(
                name: "IX_LinkClickLog_CityId",
                table: "LinkClickLog");

            migrationBuilder.DropIndex(
                name: "IX_LinkClickLog_CountryId",
                table: "LinkClickLog");

            migrationBuilder.DropIndex(
                name: "IX_LinkClickLog_StateId",
                table: "LinkClickLog");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "PublicPageEvents");

            migrationBuilder.DropColumn(
                name: "CountryId",
                table: "PublicPageEvents");

            migrationBuilder.DropColumn(
                name: "StateId",
                table: "PublicPageEvents");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "LinkClickLog");

            migrationBuilder.DropColumn(
                name: "CountryId",
                table: "LinkClickLog");

            migrationBuilder.DropColumn(
                name: "StateId",
                table: "LinkClickLog");
        }
    }
}
