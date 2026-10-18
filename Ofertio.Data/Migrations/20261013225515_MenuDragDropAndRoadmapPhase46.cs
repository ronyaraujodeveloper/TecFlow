using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ofertio.Data.Migrations
{
    /// <inheritdoc />
    public partial class MenuDragDropAndRoadmapPhase46 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Order",
                table: "MenuItems",
                newName: "OrderIndex");

            migrationBuilder.RenameIndex(
                name: "IX_MenuItems_ParentId_Order",
                table: "MenuItems",
                newName: "IX_MenuItems_ParentId_OrderIndex");

            migrationBuilder.AddColumn<string>(
                name: "Badge",
                table: "MenuItems",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCustomized",
                table: "MenuItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsFolder",
                table: "MenuItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE [MenuItems] SET [IsFolder] = 1 WHERE [Route] IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Badge",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "IsCustomized",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "IsFolder",
                table: "MenuItems");

            migrationBuilder.RenameColumn(
                name: "OrderIndex",
                table: "MenuItems",
                newName: "Order");

            migrationBuilder.RenameIndex(
                name: "IX_MenuItems_ParentId_OrderIndex",
                table: "MenuItems",
                newName: "IX_MenuItems_ParentId_Order");
        }
    }
}
