using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ofertio.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClickChannelAndBot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Channel",
                table: "LinkClickLog",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChannelOrigin",
                table: "LinkClickLog",
                type: "nvarchar(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GroupKey",
                table: "LinkClickLog",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBot",
                table: "LinkClickLog",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_LinkClickLog_TenantId_Channel_ClickedAt",
                table: "LinkClickLog",
                columns: new[] { "TenantId", "Channel", "ClickedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LinkClickLog_TenantId_ClickedAt",
                table: "LinkClickLog",
                columns: new[] { "TenantId", "ClickedAt" });

            // Reclassifica o histórico de cliques pelo User-Agent já gravado, no mesmo critério de ClickClassifierRules.
            // Só mexe em linhas ainda sem canal (Channel IS NULL), então rodar de novo não altera nada.
            migrationBuilder.Sql(@"
UPDATE LinkClickLog SET IsBot = 1
WHERE EventKind = 'Click' AND Channel IS NULL AND (
       UserAgent IS NULL OR LTRIM(RTRIM(UserAgent)) = '' OR UserAgent = 'desconhecido'
    OR LOWER(UserAgent) LIKE '%whatsapp/%'      OR LOWER(UserAgent) LIKE '%telegrambot%'
    OR LOWER(UserAgent) LIKE '%twitterbot%'     OR LOWER(UserAgent) LIKE '%facebookexternalhit%'
    OR LOWER(UserAgent) LIKE '%facebot%'        OR LOWER(UserAgent) LIKE '%slackbot%'
    OR LOWER(UserAgent) LIKE '%discordbot%'     OR LOWER(UserAgent) LIKE '%linkedinbot%'
    OR LOWER(UserAgent) LIKE '%googlebot%'      OR LOWER(UserAgent) LIKE '%bingbot%'
    OR LOWER(UserAgent) LIKE '%applebot%'       OR LOWER(UserAgent) LIKE '%bot/%'
    OR LOWER(UserAgent) LIKE '%crawler%'        OR LOWER(UserAgent) LIKE '%spider%'
    OR LOWER(UserAgent) LIKE '%preview%'        OR LOWER(UserAgent) LIKE '%curl/%'
    OR LOWER(UserAgent) LIKE '%wget/%'          OR LOWER(UserAgent) LIKE '%python-requests%'
    OR LOWER(UserAgent) LIKE '%go-http-client%' OR LOWER(UserAgent) LIKE '%headlesschrome%');");

            // Marcação antiga (tf_src): vira canal explícito.
            migrationBuilder.Sql(@"
UPDATE LinkClickLog SET
    Channel = CASE LOWER(REPLACE(SourceChannel, '-', ''))
                WHEN 'whatsapp' THEN 'WhatsApp' WHEN 'telegram' THEN 'Telegram' WHEN 'email' THEN 'Email'
                WHEN 'instagram' THEN 'Instagram' WHEN 'paginapublica' THEN 'PaginaPublica'
                ELSE LEFT(SourceChannel, 16) END,
    ChannelOrigin = 'Explicit'
WHERE EventKind = 'Click' AND Channel IS NULL AND SourceChannel IS NOT NULL AND LTRIM(RTRIM(SourceChannel)) <> '';");

            // Navegador interno de aplicativo e referer já normalizado pelo sistema (Instagram, Facebook, TikTok, WhatsApp).
            migrationBuilder.Sql(@"
UPDATE LinkClickLog SET Channel = 'Instagram', ChannelOrigin = 'InApp'
WHERE EventKind = 'Click' AND Channel IS NULL AND IsBot = 0 AND LOWER(UserAgent) LIKE '%instagram%';
UPDATE LinkClickLog SET Channel = 'Facebook', ChannelOrigin = 'InApp'
WHERE EventKind = 'Click' AND Channel IS NULL AND IsBot = 0
  AND (LOWER(UserAgent) LIKE '%fban%' OR LOWER(UserAgent) LIKE '%fbav%' OR LOWER(UserAgent) LIKE '%fb_iab%');
UPDATE LinkClickLog SET Channel = 'TikTok', ChannelOrigin = 'InApp'
WHERE EventKind = 'Click' AND Channel IS NULL AND IsBot = 0
  AND (LOWER(UserAgent) LIKE '%musical_ly%' OR LOWER(UserAgent) LIKE '%tiktok%' OR LOWER(UserAgent) LIKE '%bytedancewebview%');
UPDATE LinkClickLog SET Channel = ReferrerUrl, ChannelOrigin = 'Referer'
WHERE EventKind = 'Click' AND Channel IS NULL AND IsBot = 0 AND ReferrerUrl IN ('Instagram', 'WhatsApp', 'Facebook', 'TikTok');");

            // O restante: robô sem canal vira Desconhecido; visitante sem nenhuma pista vira Direto.
            migrationBuilder.Sql(@"
UPDATE LinkClickLog SET Channel = 'Desconhecido', ChannelOrigin = 'Unknown'
WHERE EventKind = 'Click' AND Channel IS NULL AND IsBot = 1;
UPDATE LinkClickLog SET Channel = 'Direto', ChannelOrigin = 'Unknown'
WHERE EventKind = 'Click' AND Channel IS NULL AND IsBot = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LinkClickLog_TenantId_Channel_ClickedAt",
                table: "LinkClickLog");

            migrationBuilder.DropIndex(
                name: "IX_LinkClickLog_TenantId_ClickedAt",
                table: "LinkClickLog");

            migrationBuilder.DropColumn(
                name: "Channel",
                table: "LinkClickLog");

            migrationBuilder.DropColumn(
                name: "ChannelOrigin",
                table: "LinkClickLog");

            migrationBuilder.DropColumn(
                name: "GroupKey",
                table: "LinkClickLog");

            migrationBuilder.DropColumn(
                name: "IsBot",
                table: "LinkClickLog");
        }
    }
}
