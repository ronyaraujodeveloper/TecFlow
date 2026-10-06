using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TecFlow.Database;

#nullable disable

namespace TecFlow.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261006020000_AddGroupCapturedMessageStructuredOffer")]
    public partial class AddGroupCapturedMessageStructuredOffer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CouponCode",
                table: "GroupCapturedMessages",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryProductUrl",
                table: "GroupCapturedMessages",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CouponCode",
                table: "GroupCapturedMessages");

            migrationBuilder.DropColumn(
                name: "PrimaryProductUrl",
                table: "GroupCapturedMessages");
        }
    }
}
