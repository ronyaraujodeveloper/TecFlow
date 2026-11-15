using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ofertio.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTrialPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "TrialEndingNoticeSentAt",
                table: "Tenants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostTrialPlanCode",
                table: "SaasPlans",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequireCard",
                table: "SaasPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TrialDays",
                table: "SaasPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Plano Trial: 14 dias, sem cartão, cai em Starter. Limites iguais aos do Pro para dar a melhor primeira impressão;
            // o Master pode ajustar tudo em Planos. Os menus ocultos do Pro são copiados para o Trial.
            migrationBuilder.Sql(@"
INSERT INTO SaasPlans (Code, Name, MonthlyPrice, AnnualPrice, MaxStores, DailyVipQuota, RadarEnabled, MaxDailySchedules, IsActive, SortOrder, CreatedAt, TrialDays, RequireCard, PostTrialPlanCode, Highlights)
SELECT 'Trial', 'Trial', 0, 0, p.MaxStores, p.DailyVipQuota, p.RadarEnabled, p.MaxDailySchedules, 1, 0, SYSUTCDATETIME(), 14, 0, 'Starter',
       N'Teste sem compromisso' + CHAR(10) + N'Seus dados ficam guardados ao fim do teste'
FROM SaasPlans p
WHERE p.Code = 'Pro'
  AND NOT EXISTS (SELECT 1 FROM SaasPlans t WHERE t.Code = 'Trial');");

            migrationBuilder.Sql(@"
INSERT INTO PlanMenuPermissions (PlanId, MenuItemId, IsVisible, CreatedAt)
SELECT t.Id, pm.MenuItemId, pm.IsVisible, SYSUTCDATETIME()
FROM SaasPlans t
JOIN SaasPlans p ON p.Code = 'Pro'
JOIN PlanMenuPermissions pm ON pm.PlanId = p.Id
WHERE t.Code = 'Trial'
  AND NOT EXISTS (SELECT 1 FROM PlanMenuPermissions x WHERE x.PlanId = t.Id AND x.MenuItemId = pm.MenuItemId);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM PlanMenuPermissions WHERE PlanId IN (SELECT Id FROM SaasPlans WHERE Code = 'Trial'); DELETE FROM SaasPlans WHERE Code = 'Trial';");

            migrationBuilder.DropColumn(
                name: "TrialEndingNoticeSentAt",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "PostTrialPlanCode",
                table: "SaasPlans");

            migrationBuilder.DropColumn(
                name: "RequireCard",
                table: "SaasPlans");

            migrationBuilder.DropColumn(
                name: "TrialDays",
                table: "SaasPlans");
        }
    }
}
