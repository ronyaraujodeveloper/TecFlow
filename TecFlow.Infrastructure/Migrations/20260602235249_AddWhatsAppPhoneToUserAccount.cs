using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TecFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWhatsAppPhoneToUserAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TelefoneWhatsApp",
                table: "Usuarios",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TelefoneWhatsApp",
                table: "Usuarios");
        }
    }
}
