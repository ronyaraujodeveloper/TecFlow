using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ofertio.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuItemKindAndComponents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ComponentType",
                table: "MenuItems",
                type: "nvarchar(24)",
                maxLength: 24,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "MenuItems",
                type: "int",
                nullable: false,
                defaultValue: 2);

            // Backfill da hierarquia: pastas viram Grupo (1); itens sem rota que não são pasta viram chave de
            // permissão (4); o slug personalizado vira Componente (3) da tela Minhas Páginas Públicas.
            // O catálogo padrão realinha os itens conhecidos na próxima sincronização (EnsureCatalogAsync).
            migrationBuilder.Sql("UPDATE [MenuItems] SET [Kind] = 1 WHERE [IsFolder] = 1;");
            migrationBuilder.Sql("UPDATE [MenuItems] SET [Kind] = 4 WHERE [IsFolder] = 0 AND ([Route] IS NULL OR [Route] = '');");
            migrationBuilder.Sql("UPDATE [MenuItems] SET [Kind] = 3, [ComponentType] = 'feature' WHERE [Key] = 'integracoes.paginas.slug';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ComponentType",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "MenuItems");
        }
    }
}
