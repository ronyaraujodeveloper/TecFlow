using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TecFlow.Database;

#nullable disable

namespace TecFlow.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261005030000_AddGroupCapturedMessageAvailability")]
    public partial class AddGroupCapturedMessageAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "GroupCapturedMessages",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.Sql(
                "UPDATE GroupCapturedMessages SET IsAvailable = 0 WHERE OfferStatus = 'Esgotado'");

            migrationBuilder.CreateIndex(
                name: "IX_GroupCapturedMessages_UserId_IsAvailable_ReceivedAt",
                table: "GroupCapturedMessages",
                columns: new[] { "UserId", "IsAvailable", "ReceivedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GroupCapturedMessages_UserId_IsAvailable_ReceivedAt",
                table: "GroupCapturedMessages");

            migrationBuilder.DropColumn(
                name: "IsAvailable",
                table: "GroupCapturedMessages");
        }
    }
}
