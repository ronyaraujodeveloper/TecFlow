using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TecFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLinkClickLogGenerationTelemetry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConvertedUrl",
                table: "LinkClickLog",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "LinkClickLog",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2026, 6, 9, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<string>(
                name: "EventKind",
                table: "LinkClickLog",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Click");

            migrationBuilder.AddColumn<string>(
                name: "OriginalUrl",
                table: "LinkClickLog",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Platform",
                table: "LinkClickLog",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShopId",
                table: "LinkClickLog",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "LinkClickLog",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_LinkClickLog_CreatedAt",
                table: "LinkClickLog",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LinkClickLog_TenantId",
                table: "LinkClickLog",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LinkClickLog_CreatedAt",
                table: "LinkClickLog");

            migrationBuilder.DropIndex(
                name: "IX_LinkClickLog_TenantId",
                table: "LinkClickLog");

            migrationBuilder.DropColumn(
                name: "ConvertedUrl",
                table: "LinkClickLog");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "LinkClickLog");

            migrationBuilder.DropColumn(
                name: "EventKind",
                table: "LinkClickLog");

            migrationBuilder.DropColumn(
                name: "OriginalUrl",
                table: "LinkClickLog");

            migrationBuilder.DropColumn(
                name: "Platform",
                table: "LinkClickLog");

            migrationBuilder.DropColumn(
                name: "ShopId",
                table: "LinkClickLog");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "LinkClickLog");
        }
    }
}
