using Microsoft.EntityFrameworkCore;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Master;
using TecFlow.Core.Entities;
using TecFlow.Database;
using TecFlow.Database.Entity;

namespace TecFlow.Infrastructure.Services.Master;

/// <summary>Dashboard executivo: receita recorrente, crescimento/churn e uso da plataforma nas últimas 24h.</summary>
public sealed class MasterMetricsService : IMasterMetricsService
{
    private readonly AppDbContext _context;
    private readonly IApiRequestCounter _requestCounter;

    public MasterMetricsService(AppDbContext context, IApiRequestCounter requestCounter)
    {
        _context = context;
        _requestCounter = requestCounter;
    }

    public async Task<MasterDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var since = now.AddHours(-24);
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var plans = await _context.SaasPlans.AsNoTracking().ToDictionaryAsync(item => item.Code, cancellationToken);
        var tenants = await _context.Tenants.AsNoTracking().ToListAsync(cancellationToken);
        var ownerPlans = await _context.UserAccounts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .OrderBy(item => item.CreatedAt)
            .Select(item => new { item.TenantId, item.Plan })
            .ToListAsync(cancellationToken);
        var fallbackPlan = ownerPlans
            .GroupBy(item => item.TenantId)
            .ToDictionary(group => group.Key, group => group.First().Plan);

        var snapshots = tenants.Select(tenant =>
        {
            var code = string.IsNullOrWhiteSpace(tenant.PlanCode)
                ? fallbackPlan.GetValueOrDefault(tenant.Id)
                : tenant.PlanCode;
            plans.TryGetValue(code ?? string.Empty, out var plan);
            return new SaasSubscriptionSnapshot(
                tenant.Status,
                tenant.BillingCycle,
                plan?.MonthlyPrice ?? 0m,
                plan?.AnnualPrice ?? 0m,
                tenant.CreatedAt,
                tenant.TrialEndsAt,
                tenant.CanceledAt);
        }).ToList();

        var paying = snapshots.Count(item => SaasMetricsRules.IsPaying(item.Status) && SaasMetricsRules.MonthlyEquivalent(item) > 0);
        var mrr = SaasMetricsRules.Mrr(snapshots);
        var netRevenue = await _context.SaasPayments
            .AsNoTracking()
            .Where(item => item.PaidAt != null && item.PaidAt >= monthStart)
            .SumAsync(item => (decimal?)item.NetAmount, cancellationToken) ?? 0m;

        var linksConverted = await _context.ShortAffiliateLinks
            .IgnoreQueryFilters()
            .AsNoTracking()
            .CountAsync(item => item.CreatedAt >= since, cancellationToken);
        var clicks = await _context.LinkClickLogs
            .IgnoreQueryFilters()
            .AsNoTracking()
            .CountAsync(item => item.EventKind == LinkClickLog.EventKindClick && item.ClickedAt >= since, cancellationToken);
        var whatsApp = await _context.WhatsAppBroadcastCampaigns
            .IgnoreQueryFilters()
            .AsNoTracking()
            .CountAsync(item => item.Status == WhatsAppBroadcastStatuses.Completed
                                && (item.UpdatedAt ?? item.ScheduledAt) >= since, cancellationToken);
        var telegram = await _context.TelegramBroadcastCampaigns
            .IgnoreQueryFilters()
            .AsNoTracking()
            .CountAsync(item => item.Status == TelegramBroadcastStatuses.Completed
                                && (item.UpdatedAt ?? item.ScheduledAt) >= since, cancellationToken);

        return new MasterDashboardDto
        {
            GeneratedAt = now,
            Mrr = mrr,
            Arr = SaasMetricsRules.Arr(mrr),
            Arpu = SaasMetricsRules.Arpu(mrr, paying),
            NetRevenueMonth = netRevenue,
            PayingTenants = paying,
            TrialTenants = snapshots.Count(item => TenantStatuses.Normalize(item.Status) == TenantStatuses.Trial),
            OverdueTenants = snapshots.Count(item => TenantStatuses.Normalize(item.Status) == TenantStatuses.Inadimplente),
            TotalTenants = snapshots.Count,
            TrialConversionPercent = SaasMetricsRules.TrialConversionPercent(snapshots, now),
            ChurnRatePercent = SaasMetricsRules.ChurnRatePercent(snapshots, now),
            NewSubscribers = SaasMetricsRules.NewSubscribersByMonth(snapshots.Select(item => item.CreatedAt), now)
                .Select(item => new MasterMonthlyPointDto { Month = item.Month, Count = item.Count })
                .ToList(),
            LinksConverted24h = linksConverted,
            LinkClicks24h = clicks,
            WhatsAppDispatches24h = whatsApp,
            TelegramDispatches24h = telegram,
            ApiRequests24h = _requestCounter.CountLast24Hours()
        };
    }
}
