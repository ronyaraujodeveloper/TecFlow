using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TecFlow.Database;

#nullable disable

namespace TecFlow.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261005010000_AddGroupCapturedMessageCuration")]
    public partial class AddGroupCapturedMessageCuration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasDirectProductUrl",
                table: "GroupCapturedMessages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsIgnored",
                table: "GroupCapturedMessages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "IgnoredAt",
                table: "GroupCapturedMessages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE GroupCapturedMessages SET HasDirectProductUrl = 1 WHERE PlatformType IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GroupCapturedMessages_UserId_IsIgnored_ReceivedAt",
                table: "GroupCapturedMessages",
                columns: new[] { "UserId", "IsIgnored", "ReceivedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GroupCapturedMessages_UserId_IsIgnored_ReceivedAt",
                table: "GroupCapturedMessages");

            migrationBuilder.DropColumn(
                name: "HasDirectProductUrl",
                table: "GroupCapturedMessages");

            migrationBuilder.DropColumn(
                name: "IsIgnored",
                table: "GroupCapturedMessages");

            migrationBuilder.DropColumn(
                name: "IgnoredAt",
                table: "GroupCapturedMessages");
        }
    }
}
