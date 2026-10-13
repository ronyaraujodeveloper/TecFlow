using TecFlow.Business.Service.Master;

namespace TecFlow.Tests.Unit.Master;

public class MasterPermissionMatrixTests
{
    [Fact]
    public void SuperAdmin_ShouldHaveEveryPermission()
    {
        Assert.All(MasterPermissions.All, permission => Assert.True(MasterPermissionMatrix.Has(MasterRoles.SuperAdmin, permission)));
    }

    [Theory]
    [InlineData(MasterPermissions.ManageGlobalKeys)]
    [InlineData(MasterPermissions.ManageOperators)]
    [InlineData(MasterPermissions.AdjustCredits)]
    [InlineData(MasterPermissions.ManagePlans)]
    public void SuporteTecnico_ShouldNotTouchKeysOperatorsOrMoney(string permission)
    {
        Assert.False(MasterPermissionMatrix.Has(MasterRoles.SuporteTecnico, permission));
    }

    [Theory]
    [InlineData(MasterPermissions.Impersonate)]
    [InlineData(MasterPermissions.ManageTenants)]
    [InlineData(MasterPermissions.ManageGlobalKeys)]
    public void FinanceiroSaaS_ShouldNotModerateOrEmulate(string permission)
    {
        Assert.False(MasterPermissionMatrix.Has(MasterRoles.FinanceiroSaaS, permission));
    }

    [Fact]
    public void TenantUser_ShouldHaveNoPermission()
    {
        Assert.Empty(MasterPermissionMatrix.PermissionsFor(MasterRoles.TenantUser));
        Assert.Empty(MasterPermissionMatrix.PermissionsFor(null));
    }

    [Fact]
    public void RolesFor_ShouldListRolesAllowedForPolicy()
    {
        var roles = MasterPermissionMatrix.RolesFor(MasterPermissions.AdjustCredits);

        Assert.Equal([MasterRoles.SuperAdmin, MasterRoles.FinanceiroSaaS], roles);
    }

    [Theory]
    [InlineData("superadmin", MasterRoles.SuperAdmin)]
    [InlineData(" FinanceiroSaaS ", MasterRoles.FinanceiroSaaS)]
    [InlineData("Master", MasterRoles.TenantUser)]
    [InlineData(null, MasterRoles.TenantUser)]
    public void Normalize_ShouldMapKnownRolesCaseInsensitive(string? input, string expected)
    {
        Assert.Equal(expected, MasterRoles.Normalize(input));
    }

    [Fact]
    public void PolicyConstants_ShouldMatchPolicyName()
    {
        Assert.Equal(MasterPermissions.PolicyName(MasterPermissions.ViewAudit), MasterPolicies.ViewAudit);
    }
}

public class TenantLifecycleRulesTests
{
    private static readonly DateTime Now = new(2026, 10, 12, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(TenantStatuses.Pausado, true)]
    [InlineData(TenantStatuses.Suspenso, true)]
    [InlineData(TenantStatuses.Cancelado, true)]
    [InlineData(TenantStatuses.Inadimplente, false)]
    [InlineData(TenantStatuses.Trial, false)]
    [InlineData(null, false)]
    public void IsAccessBlocked_ShouldBlockOnlyPausedSuspendedOrCanceled(string? status, bool expected)
    {
        Assert.Equal(expected, TenantLifecycleRules.IsAccessBlocked(status));
    }

    [Theory]
    [InlineData(TenantLifecycleActions.Pausar, true)]
    [InlineData(TenantLifecycleActions.Suspender, true)]
    [InlineData(TenantLifecycleActions.Cancelar, true)]
    [InlineData(TenantLifecycleActions.Reativar, false)]
    [InlineData(TenantLifecycleActions.EstenderTrial, false)]
    public void RequiresReason_ShouldDemandJustificationForRestrictiveActions(string action, bool expected)
    {
        Assert.Equal(expected, TenantLifecycleRules.RequiresReason(action));
    }

    [Fact]
    public void Apply_Cancel_ShouldStampCanceledAt()
    {
        var transition = TenantLifecycleRules.Apply(TenantStatuses.Ativo, TenantLifecycleActions.Cancelar, Now, null);

        Assert.NotNull(transition);
        Assert.Equal(TenantStatuses.Cancelado, transition.Status);
        Assert.Equal(Now, transition.CanceledAt);
    }

    [Fact]
    public void Apply_OnCanceledTenant_ShouldOnlyAllowReactivation()
    {
        Assert.Null(TenantLifecycleRules.Apply(TenantStatuses.Cancelado, TenantLifecycleActions.Pausar, Now, null));
        Assert.Equal(
            TenantStatuses.Ativo,
            TenantLifecycleRules.Apply(TenantStatuses.Cancelado, TenantLifecycleActions.Reativar, Now, null)!.Status);
    }

    [Fact]
    public void Apply_Reactivate_WithFutureTrial_ShouldReturnToTrial()
    {
        var transition = TenantLifecycleRules.Apply(TenantStatuses.Pausado, TenantLifecycleActions.Reativar, Now, Now.AddDays(3));

        Assert.Equal(TenantStatuses.Trial, transition!.Status);
    }

    [Fact]
    public void Apply_ExtendTrial_ShouldAddDaysFromCurrentEndWhenStillRunning()
    {
        var transition = TenantLifecycleRules.Apply(TenantStatuses.Trial, TenantLifecycleActions.EstenderTrial, Now, Now.AddDays(2), 10);

        Assert.Equal(Now.AddDays(12), transition!.TrialEndsAt);
    }

    [Fact]
    public void Apply_ExtendTrial_AfterExpiry_ShouldUseDefaultDaysFromNow()
    {
        var transition = TenantLifecycleRules.Apply(TenantStatuses.Inadimplente, TenantLifecycleActions.EstenderTrial, Now, Now.AddDays(-5));

        Assert.Equal(TenantStatuses.Trial, transition!.Status);
        Assert.Equal(Now.AddDays(TenantLifecycleRules.DefaultTrialExtensionDays), transition.TrialEndsAt);
    }

    [Fact]
    public void Apply_UnknownAction_ShouldReturnNull()
    {
        Assert.Null(TenantLifecycleRules.Apply(TenantStatuses.Ativo, "excluir", Now, null));
    }

    [Fact]
    public void ShouldAutoSuspend_ShouldRespectGracePeriod()
    {
        Assert.False(TenantLifecycleRules.ShouldAutoSuspend(TenantStatuses.Inadimplente, Now.AddDays(-4), Now));
        Assert.True(TenantLifecycleRules.ShouldAutoSuspend(TenantStatuses.Inadimplente, Now.AddDays(-TenantLifecycleRules.OverdueGraceDays), Now));
        Assert.False(TenantLifecycleRules.ShouldAutoSuspend(TenantStatuses.Ativo, Now.AddDays(-30), Now));
    }

    [Fact]
    public void ShouldExpireTrial_ShouldIgnoreTenantsWithGatewaySubscription()
    {
        Assert.True(TenantLifecycleRules.ShouldExpireTrial(TenantStatuses.Trial, Now.AddMinutes(-1), null, Now));
        Assert.False(TenantLifecycleRules.ShouldExpireTrial(TenantStatuses.Trial, Now.AddMinutes(-1), "sub_123", Now));
        Assert.False(TenantLifecycleRules.ShouldExpireTrial(TenantStatuses.Trial, Now.AddDays(1), null, Now));
    }

    [Fact]
    public void FormatBlockedMessage_ShouldMentionStatus()
    {
        Assert.Contains("suspenso", TenantLifecycleRules.FormatBlockedMessage(TenantStatuses.Suspenso));
    }
}

public class SaasMetricsRulesTests
{
    private static readonly DateTime Now = new(2026, 10, 12, 12, 0, 0, DateTimeKind.Utc);

    private static SaasSubscriptionSnapshot Snapshot(
        string status,
        string cycle = SaasMetricsRules.CycleMonthly,
        decimal monthly = 100m,
        decimal annual = 1200m,
        DateTime? created = null,
        DateTime? trialEnds = null,
        DateTime? canceled = null) =>
        new(status, cycle, monthly, annual, created ?? Now.AddMonths(-2), trialEnds, canceled);

    [Fact]
    public void Mrr_ShouldSumPayingTenantsAndNormalizeAnnualPlans()
    {
        var items = new[]
        {
            Snapshot(TenantStatuses.Ativo, monthly: 99.90m),
            Snapshot(TenantStatuses.Ativo, SaasMetricsRules.CycleAnnual, annual: 999m),
            Snapshot(TenantStatuses.Inadimplente, monthly: 49.90m),
            Snapshot(TenantStatuses.Trial, monthly: 500m),
            Snapshot(TenantStatuses.Cancelado, monthly: 500m)
        };

        var mrr = SaasMetricsRules.Mrr(items);

        Assert.Equal(99.90m + 83.25m + 49.90m, mrr);
        Assert.Equal(mrr * 12m, SaasMetricsRules.Arr(mrr));
    }

    [Fact]
    public void Arpu_ShouldBeZeroWithoutPayingTenants()
    {
        Assert.Equal(0m, SaasMetricsRules.Arpu(100m, 0));
        Assert.Equal(50m, SaasMetricsRules.Arpu(100m, 2));
    }

    [Fact]
    public void ChurnRate_ShouldUseCancellationsInLast30Days()
    {
        var items = new[]
        {
            Snapshot(TenantStatuses.Ativo),
            Snapshot(TenantStatuses.Ativo),
            Snapshot(TenantStatuses.Ativo),
            Snapshot(TenantStatuses.Cancelado, canceled: Now.AddDays(-10)),
            Snapshot(TenantStatuses.Cancelado, canceled: Now.AddDays(-60))
        };

        Assert.Equal(25m, SaasMetricsRules.ChurnRatePercent(items, Now));
    }

    [Fact]
    public void TrialConversion_ShouldConsiderOnlyFinishedTrials()
    {
        var items = new[]
        {
            Snapshot(TenantStatuses.Ativo, trialEnds: Now.AddDays(-20)),
            Snapshot(TenantStatuses.Cancelado, trialEnds: Now.AddDays(-15)),
            Snapshot(TenantStatuses.Trial, trialEnds: Now.AddDays(5))
        };

        Assert.Equal(50m, SaasMetricsRules.TrialConversionPercent(items, Now));
        Assert.Equal(0m, SaasMetricsRules.TrialConversionPercent([], Now));
    }

    [Fact]
    public void NewSubscribersByMonth_ShouldReturnSixBucketsEndingInCurrentMonth()
    {
        var created = new[] { Now, Now.AddDays(-1), Now.AddMonths(-2), Now.AddMonths(-12) };

        var points = SaasMetricsRules.NewSubscribersByMonth(created, Now);

        Assert.Equal(6, points.Count);
        Assert.Equal("2026-10", points[^1].Month);
        Assert.Equal(2, points[^1].Count);
        Assert.Equal(1, points[^3].Count);
        Assert.Equal(3, points.Sum(point => point.Count));
    }
}

public class PlanLimitRulesTests
{
    [Theory]
    [InlineData(0, 100, false)]
    [InlineData(2, 1, false)]
    [InlineData(2, 2, true)]
    [InlineData(2, 5, true)]
    public void ExceedsCount_ShouldTreatZeroAsUnlimited(int limit, int current, bool expected)
    {
        Assert.Equal(expected, PlanLimitRules.ExceedsCount(limit, current));
    }

    [Fact]
    public void EffectiveDailyQuota_ShouldUsePlanQuotaOrFallbacks()
    {
        Assert.Equal(5, PlanLimitRules.EffectiveDailyQuota(PlanLimits.Unlimited, 5));
        Assert.Equal(30, PlanLimitRules.EffectiveDailyQuota(new PlanLimits(SaasPlanCodes.Pro, 10, 30, true, 50), 5));
        Assert.Equal(
            PlanLimitRules.UnlimitedDailyQuota,
            PlanLimitRules.EffectiveDailyQuota(new PlanLimits(SaasPlanCodes.Enterprise, 0, 0, true, 0), 5));
    }

    [Fact]
    public void Messages_ShouldMentionPlanAndLimit()
    {
        var limits = new PlanLimits(SaasPlanCodes.Starter, 2, 5, false, 10);

        Assert.Contains("Starter", PlanLimitRules.StoresLimitMessage(limits));
        Assert.Contains("2", PlanLimitRules.StoresLimitMessage(limits));
        Assert.Contains("10", PlanLimitRules.SchedulesLimitMessage(limits));
        Assert.Contains("Radar", PlanLimitRules.RadarBlockedMessage(limits));
    }
}

public class SystemSettingCatalogTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("abc", "••••")]
    [InlineData("sk-1234567890", "••••7890")]
    public void Mask_ShouldRevealOnlyLastFourChars(string? value, string expected)
    {
        Assert.Equal(expected, SystemSettingCatalog.Mask(value));
    }

    [Fact]
    public void Find_ShouldBeCaseInsensitive()
    {
        var definition = SystemSettingCatalog.Find("payments:asaas:apikey");

        Assert.NotNull(definition);
        Assert.True(definition.IsSecret);
        Assert.Null(SystemSettingCatalog.Find("Desconhecida:Chave"));
    }

    [Fact]
    public void Catalog_ShouldHaveUniqueKeysAndCoverVaultSecrets()
    {
        var keys = SystemSettingCatalog.All.Select(item => item.Key).ToList();

        Assert.Equal(keys.Count, keys.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Contains("OpenAI:ApiKey", keys);
        Assert.Contains(SystemSettingCatalog.ShortLinkDomain, keys);
    }
}
