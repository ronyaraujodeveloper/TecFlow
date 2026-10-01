using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TecFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWhatsAppIntegrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WhatsAppIntegrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    InstanceName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ConnectionStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ProfileName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    LastConnectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppIntegrations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppIntegrations_InstanceName",
                table: "WhatsAppIntegrations",
                column: "InstanceName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppIntegrations_UserId",
                table: "WhatsAppIntegrations",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WhatsAppIntegrations");
        }
    }
}
