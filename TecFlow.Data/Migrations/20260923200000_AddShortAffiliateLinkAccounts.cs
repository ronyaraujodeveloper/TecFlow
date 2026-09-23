using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TecFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShortAffiliateLinkAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LinkGroupId",
                table: "ShortAffiliateLinks",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql(
                """
                UPDATE ShortAffiliateLinks
                SET LinkGroupId = AffiliateLinkId
                WHERE LinkGroupId = '00000000-0000-0000-0000-000000000000';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ShortAffiliateLinks_LinkGroupId",
                table: "ShortAffiliateLinks",
                column: "LinkGroupId");

            migrationBuilder.CreateTable(
                name: "ShortAffiliateLinkAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LinkGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShortAffiliateLinkId = table.Column<int>(type: "int", nullable: false),
                    IntegracaoLojaId = table.Column<int>(type: "int", nullable: false),
                    MarketplaceAccountId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShortAffiliateLinkAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShortAffiliateLinkAccounts_ShortAffiliateLinks_ShortAffiliateLinkId",
                        column: x => x.ShortAffiliateLinkId,
                        principalTable: "ShortAffiliateLinks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShortAffiliateLinkAccounts_LinkGroupId",
                table: "ShortAffiliateLinkAccounts",
                column: "LinkGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ShortAffiliateLinkAccounts_LinkGroupId_IntegracaoLojaId",
                table: "ShortAffiliateLinkAccounts",
                columns: new[] { "LinkGroupId", "IntegracaoLojaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShortAffiliateLinkAccounts_ShortAffiliateLinkId",
                table: "ShortAffiliateLinkAccounts",
                column: "ShortAffiliateLinkId");

            migrationBuilder.Sql(
                """
                INSERT INTO ShortAffiliateLinkAccounts
                    (TenantId, LinkGroupId, ShortAffiliateLinkId, IntegracaoLojaId, MarketplaceAccountId, IsActive, CreatedAt, UpdatedAt)
                SELECT
                    TenantId,
                    LinkGroupId,
                    Id,
                    ISNULL(IntegracaoLojaId, 0),
                    MarketplaceAccountId,
                    IsActive,
                    CreatedAt,
                    UpdatedAt
                FROM ShortAffiliateLinks
                WHERE IntegracaoLojaId IS NOT NULL AND IntegracaoLojaId > 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ShortAffiliateLinkAccounts");
            migrationBuilder.DropIndex(name: "IX_ShortAffiliateLinks_LinkGroupId", table: "ShortAffiliateLinks");
            migrationBuilder.DropColumn(name: "LinkGroupId", table: "ShortAffiliateLinks");
        }
    }
}
