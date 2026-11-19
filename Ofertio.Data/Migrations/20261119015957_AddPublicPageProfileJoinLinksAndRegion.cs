using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ofertio.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicPageProfileJoinLinksAndRegion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxJoinLinks",
                table: "SaasPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "LinkClickLog",
                type: "nvarchar(96)",
                maxLength: 96,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "LinkClickLog",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "LinkClickLog",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PublicPageEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    JoinLinkId = table.Column<int>(type: "int", nullable: true),
                    Channel = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    IsBot = table.Column<bool>(type: "bit", nullable: false),
                    IpMasked = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    DeviceType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Region = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    City = table.Column<string>(type: "nvarchar(96)", maxLength: 96, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicPageEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PublicPageJoinLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    InviteUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    SourceGroupId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    ShowMemberCount = table.Column<bool>(type: "bit", nullable: false),
                    MemberCount = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    StatusReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FailedChecks = table.Column<int>(type: "int", nullable: false),
                    LastCheckedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicPageJoinLinks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PublicPageProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AvatarUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicPageProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PublicPageEvents_TenantId_Kind_CreatedAt",
                table: "PublicPageEvents",
                columns: new[] { "TenantId", "Kind", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PublicPageEvents_TenantId_UserId_CreatedAt",
                table: "PublicPageEvents",
                columns: new[] { "TenantId", "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PublicPageJoinLinks_Status_LastCheckedAt",
                table: "PublicPageJoinLinks",
                columns: new[] { "Status", "LastCheckedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PublicPageJoinLinks_UserId_Status",
                table: "PublicPageJoinLinks",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PublicPageProfiles_UserId",
                table: "PublicPageProfiles",
                column: "UserId",
                unique: true);

            // Limite padrão de links de convite por plano (editável em Master > Planos): Trial 2, Starter 3, Pro 10, Agency 30, Enterprise ilimitado.
            migrationBuilder.Sql(@"
UPDATE SaasPlans SET MaxJoinLinks = CASE Code
    WHEN 'Trial' THEN 2 WHEN 'Starter' THEN 3 WHEN 'Pro' THEN 10 WHEN 'Agency' THEN 30 WHEN 'Enterprise' THEN 0
    ELSE MaxJoinLinks END
WHERE MaxJoinLinks = 0 AND Code IN ('Trial', 'Starter', 'Pro', 'Agency');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PublicPageEvents");

            migrationBuilder.DropTable(
                name: "PublicPageJoinLinks");

            migrationBuilder.DropTable(
                name: "PublicPageProfiles");

            migrationBuilder.DropColumn(
                name: "MaxJoinLinks",
                table: "SaasPlans");

            migrationBuilder.DropColumn(
                name: "City",
                table: "LinkClickLog");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "LinkClickLog");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "LinkClickLog");
        }
    }
}
