using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Ofertio.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductTags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OfferTags",
                columns: table => new
                {
                    OfferId = table.Column<int>(type: "int", nullable: false),
                    ProductTagId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfferTags", x => new { x.OfferId, x.ProductTagId });
                    table.ForeignKey(
                        name: "FK_OfferTags_GroupCapturedMessages_OfferId",
                        column: x => x.OfferId,
                        principalTable: "GroupCapturedMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OfferTags_ProductTags_ProductTagId",
                        column: x => x.ProductTagId,
                        principalTable: "ProductTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ProductTags",
                columns: new[] { "Id", "IsActive", "Name", "Slug" },
                values: new object[,]
                {
                    { 1, true, "Gamer", "gamer" },
                    { 2, true, "Eletrônicos", "eletronicos" },
                    { 3, true, "Informática", "informatica" },
                    { 4, true, "Casa & Cozinha", "casa-cozinha" },
                    { 5, true, "Smartphones", "smartphones" },
                    { 6, true, "Moda/Beleza", "moda-beleza" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_OfferTags_ProductTagId",
                table: "OfferTags",
                column: "ProductTagId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductTags_Slug",
                table: "ProductTags",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OfferTags");

            migrationBuilder.DropTable(
                name: "ProductTags");
        }
    }
}
