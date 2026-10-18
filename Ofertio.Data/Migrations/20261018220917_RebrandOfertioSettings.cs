using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ofertio.Data.Migrations
{
    /// <inheritdoc />
    public partial class RebrandOfertioSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE [SystemSettings]
                SET [Key] = N'Ofertio:ShortLinks:PublicBaseUrl'
                WHERE [Key] = N'TecFlow:ShortLinks:PublicBaseUrl'
                  AND NOT EXISTS (SELECT 1 FROM [SystemSettings] WHERE [Key] = N'Ofertio:ShortLinks:PublicBaseUrl');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE [SystemSettings]
                SET [Key] = N'TecFlow:ShortLinks:PublicBaseUrl'
                WHERE [Key] = N'Ofertio:ShortLinks:PublicBaseUrl'
                  AND NOT EXISTS (SELECT 1 FROM [SystemSettings] WHERE [Key] = N'TecFlow:ShortLinks:PublicBaseUrl');
                """);
        }
    }
}
