using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TecFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShortAffiliateLinkAndLinkClickLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShortAffiliateLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AffiliateLinkId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShortCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    DestinationUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    OriginalUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    PlatformType = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    IntegracaoLojaId = table.Column<int>(type: "integer", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomNickname = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShortAffiliateLinks", x => x.Id);
                    table.UniqueConstraint("AK_ShortAffiliateLinks_AffiliateLinkId", x => x.AffiliateLinkId);
                });

            migrationBuilder.CreateTable(
                name: "LinkClickLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AffiliateLinkId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClickedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    DeviceType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ReferrerUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinkClickLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LinkClickLog_ShortAffiliateLinks_AffiliateLinkId",
                        column: x => x.AffiliateLinkId,
                        principalTable: "ShortAffiliateLinks",
                        principalColumn: "AffiliateLinkId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LinkClickLog_AffiliateLinkId",
                table: "LinkClickLog",
                column: "AffiliateLinkId");

            migrationBuilder.CreateIndex(
                name: "IX_LinkClickLog_ClickedAt",
                table: "LinkClickLog",
                column: "ClickedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ShortAffiliateLinks_AffiliateLinkId",
                table: "ShortAffiliateLinks",
                column: "AffiliateLinkId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShortAffiliateLinks_ShortCode",
                table: "ShortAffiliateLinks",
                column: "ShortCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShortAffiliateLinks_UserId_CreatedAt",
                table: "ShortAffiliateLinks",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LinkClickLog");

            migrationBuilder.DropTable(
                name: "ShortAffiliateLinks");
        }
    }
}
