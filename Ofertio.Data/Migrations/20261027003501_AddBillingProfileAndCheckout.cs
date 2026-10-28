using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ofertio.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingProfileAndCheckout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Highlights",
                table: "SaasPlans",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PedidosAssinatura",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PlanCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PlanName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BillingCycle = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    CardBrand = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    CardLast4 = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    GatewayPaymentId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidosAssinatura", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PerfisCadastrais",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PersonType = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    ResponsibleName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    Document = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    Street = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    Number = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    Complement = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    District = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    City = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    State = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfisCadastrais", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PedidosAssinatura_UserId_CreatedAt",
                table: "PedidosAssinatura",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PerfisCadastrais_UserId",
                table: "PerfisCadastrais",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PedidosAssinatura");

            migrationBuilder.DropTable(
                name: "PerfisCadastrais");

            migrationBuilder.DropColumn(
                name: "Highlights",
                table: "SaasPlans");
        }
    }
}
