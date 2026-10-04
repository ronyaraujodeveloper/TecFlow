using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TecFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupCapturedMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GroupCapturedMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    GroupKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ExternalMessageId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RawText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MediaUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProductImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OriginalUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ExtractedPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ValidatedPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PlatformType = table.Column<int>(type: "int", nullable: true),
                    PlatformName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    OfferStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastValidatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupCapturedMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GroupCapturedMessages_UserId_Channel_ExternalMessageId_OriginalUrl",
                table: "GroupCapturedMessages",
                columns: new[] { "UserId", "Channel", "ExternalMessageId", "OriginalUrl" });

            migrationBuilder.CreateIndex(
                name: "IX_GroupCapturedMessages_UserId_GroupKey_ReceivedAt",
                table: "GroupCapturedMessages",
                columns: new[] { "UserId", "GroupKey", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GroupCapturedMessages_UserId_ReceivedAt",
                table: "GroupCapturedMessages",
                columns: new[] { "UserId", "ReceivedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupCapturedMessages");
        }
    }
}
