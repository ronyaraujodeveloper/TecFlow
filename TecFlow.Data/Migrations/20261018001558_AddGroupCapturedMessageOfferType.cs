using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TecFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupCapturedMessageOfferType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "GroupCapturedMessages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_GroupCapturedMessages_UserId_Type_IsIgnored_ReceivedAt",
                table: "GroupCapturedMessages",
                columns: new[] { "UserId", "Type", "IsIgnored", "ReceivedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GroupCapturedMessages_UserId_Type_IsIgnored_ReceivedAt",
                table: "GroupCapturedMessages");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "GroupCapturedMessages");
        }
    }
}
