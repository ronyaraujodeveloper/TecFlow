using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TecFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketplaceTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketplaceTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ShopId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    MarketplaceType = table.Column<int>(type: "integer", nullable: false),
                    AccessToken = table.Column<string>(type: "text", nullable: false),
                    RefreshToken = table.Column<string>(type: "text", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RefreshExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketplaceTokens", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceTokens_ShopId_MarketplaceType",
                table: "MarketplaceTokens",
                columns: new[] { "ShopId", "MarketplaceType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketplaceTokens");
        }
    }
}
