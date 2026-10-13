namespace TecFlow.Business.Service.Master;

public sealed record SaasSubscriptionSnapshot(
    string Status,
    string BillingCycle,
    decimal MonthlyPrice,
    decimal AnnualPrice,
    DateTime CreatedAt,
    DateTime? TrialEndsAt,
    DateTime? CanceledAt);

/// <summary>Cálculos de MRR, ARR, ARPU, churn e conversão de trial.</summary>
public static class SaasMetricsRules
{
    public const string CycleAnnual = "Anual";
    public const string CycleMonthly = "Mensal";

    public static bool IsPaying(string? status) =>
        TenantStatuses.Normalize(status) is TenantStatuses.Ativo or TenantStatuses.Inadimplente;

    public static decimal MonthlyEquivalent(SaasSubscriptionSnapshot item) =>
        string.Equals(item.BillingCycle, CycleAnnual, StringComparison.OrdinalIgnoreCase)
            ? Math.Round(item.AnnualPrice / 12m, 2)
            : item.MonthlyPrice;

    public static decimal Mrr(IEnumerable<SaasSubscriptionSnapshot> items) =>
        items.Where(item => IsPaying(item.Status)).Sum(MonthlyEquivalent);

    public static decimal Arr(decimal mrr) => mrr * 12m;

    public static decimal Arpu(decimal mrr, int payingCount) =>
        payingCount <= 0 ? 0m : Math.Round(mrr / payingCount, 2);

    /// <summary>Cancelados nos últimos 30 dias sobre (pagantes atuais + cancelados no período).</summary>
    public static decimal ChurnRatePercent(IReadOnlyCollection<SaasSubscriptionSnapshot> items, DateTime now)
    {
        var since = now.AddDays(-30);
        var canceled = items.Count(item => item.CanceledAt >= since && item.CanceledAt <= now);
        var paying = items.Count(item => IsPaying(item.Status));
        var baseCount = paying + canceled;
        return baseCount == 0 ? 0m : Math.Round(canceled * 100m / baseCount, 1);
    }

    /// <summary>Trials encerrados que hoje estão pagantes sobre o total de trials encerrados.</summary>
    public static decimal TrialConversionPercent(IReadOnlyCollection<SaasSubscriptionSnapshot> items, DateTime now)
    {
        var finished = items.Where(item => item.TrialEndsAt.HasValue && item.TrialEndsAt.Value <= now).ToList();
        if (finished.Count == 0)
        {
            return 0m;
        }

        var converted = finished.Count(item => TenantStatuses.Normalize(item.Status) == TenantStatuses.Ativo);
        return Math.Round(converted * 100m / finished.Count, 1);
    }

    /// <summary>Novos inquilinos por mês (yyyy-MM), do mais antigo ao atual.</summary>
    public static IReadOnlyList<(string Month, int Count)> NewSubscribersByMonth(
        IEnumerable<DateTime> createdAt,
        DateTime now,
        int months = 6)
    {
        var dates = createdAt.ToList();
        var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-(months - 1));
        return Enumerable.Range(0, months)
            .Select(offset =>
            {
                var month = start.AddMonths(offset);
                var next = month.AddMonths(1);
                return (month.ToString("yyyy-MM"), dates.Count(date => date >= month && date < next));
            })
            .ToList();
    }
}
