using Microsoft.EntityFrameworkCore;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Master;
using Ofertio.Core.Entities;
using Ofertio.Database;

namespace Ofertio.Infrastructure.Services.Master;

public sealed class SaasPlanService : ISaasPlanService, IPlanLimitsService
{
    private readonly AppDbContext _context;
    private readonly IMasterAuditService? _audit;

    public SaasPlanService(AppDbContext context, IMasterAuditService? audit = null)
    {
        _context = context;
        _audit = audit;
    }

    public async Task<IReadOnlyList<SaasPlanDto>> ListAsync(bool includeInactive = true, CancellationToken cancellationToken = default)
    {
        var plans = await _context.SaasPlans
            .AsNoTracking()
            .Where(item => includeInactive || item.IsActive)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);
        var tenantCounts = await _context.Tenants
            .AsNoTracking()
            .Where(item => item.PlanCode != null)
            .GroupBy(item => item.PlanCode!)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Key, item => item.Count, cancellationToken);

        return plans.Select(plan =>
        {
            var dto = Map(plan);
            dto.TenantCount = tenantCounts.TryGetValue(plan.Code, out var count) ? count : 0;
            return dto;
        }).ToList();
    }

    public async Task<(SaasPlanDto? Item, string? Error)> SaveAsync(
        MasterActor actor,
        SaasPlanDto request,
        CancellationToken cancellationToken = default)
    {
        var code = request.Code?.Trim() ?? string.Empty;
        if (code.Length is 0 or > 32 || string.IsNullOrWhiteSpace(request.Name))
        {
            return (null, "Informe código (até 32 caracteres) e nome do plano.");
        }

        if (request.MonthlyPrice < 0 || request.AnnualPrice < 0
            || request.MaxStores < 0 || request.DailyVipQuota < 0 || request.MaxDailySchedules < 0)
        {
            return (null, "Preços e limites não podem ser negativos (use 0 para ilimitado).");
        }

        var plan = request.Id > 0
            ? await _context.SaasPlans.FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            : await _context.SaasPlans.FirstOrDefaultAsync(item => item.Code == code, cancellationToken);
        var isNew = plan is null;
        if (plan is null)
        {
            plan = new SaasPlan { Code = code };
            _context.SaasPlans.Add(plan);
        }
        else if (!string.Equals(plan.Code, code, StringComparison.Ordinal)
                 && await _context.SaasPlans.AnyAsync(item => item.Code == code && item.Id != plan.Id, cancellationToken))
        {
            return (null, "Já existe outro plano com este código.");
        }

        plan.Code = code;
        plan.Name = request.Name.Trim();
        plan.MonthlyPrice = request.MonthlyPrice;
        plan.AnnualPrice = request.AnnualPrice;
        plan.MaxStores = request.MaxStores;
        plan.DailyVipQuota = request.DailyVipQuota;
        plan.RadarEnabled = request.RadarEnabled;
        plan.MaxDailySchedules = request.MaxDailySchedules;
        plan.IsActive = request.IsActive;
        plan.SortOrder = request.SortOrder;
        plan.Touch();
        await _context.SaveChangesAsync(cancellationToken);

        if (_audit is not null)
        {
            await _audit.LogAsync(actor, MasterAuditActions.PlanSaved, "SaasPlan", plan.Id.ToString(),
                $"{(isNew ? "Criado" : "Alterado")} plano {plan.Code}: R$ {plan.MonthlyPrice:N2}/mês, R$ {plan.AnnualPrice:N2}/ano, " +
                $"lojas {plan.MaxStores}, VIP {plan.DailyVipQuota}/dia, radar {(plan.RadarEnabled ? "sim" : "não")}, disparos {plan.MaxDailySchedules}/dia.",
                cancellationToken);
        }

        return (Map(plan), null);
    }

    public async Task<PlanLimits> GetForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return PlanLimits.Unlimited;
        }

        var user = await _context.UserAccounts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(item => item.Id == userId)
            .Select(item => new { item.TenantId, item.Plan, item.Role })
            .FirstOrDefaultAsync(cancellationToken);
        if (user is null || MasterRoles.IsMaster(user.Role))
        {
            return PlanLimits.Unlimited;
        }

        var tenantPlan = await _context.Tenants
            .AsNoTracking()
            .Where(item => item.Id == user.TenantId)
            .Select(item => item.PlanCode)
            .FirstOrDefaultAsync(cancellationToken);
        var code = string.IsNullOrWhiteSpace(tenantPlan) ? user.Plan : tenantPlan;
        return await ResolveAsync(code, cancellationToken);
    }

    internal async Task<PlanLimits> ResolveAsync(string? planCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(planCode))
        {
            return PlanLimits.Unlimited;
        }

        var code = planCode.Trim();
        var plan = await _context.SaasPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Code == code && item.IsActive, cancellationToken);
        return plan is null ? PlanLimits.Unlimited : ToLimits(plan);
    }

    public static PlanLimits ToLimits(SaasPlan plan) =>
        new(plan.Code, plan.MaxStores, plan.DailyVipQuota, plan.RadarEnabled, plan.MaxDailySchedules);

    private static SaasPlanDto Map(SaasPlan plan) =>
        new()
        {
            Id = plan.Id,
            Code = plan.Code,
            Name = plan.Name,
            MonthlyPrice = plan.MonthlyPrice,
            AnnualPrice = plan.AnnualPrice,
            MaxStores = plan.MaxStores,
            DailyVipQuota = plan.DailyVipQuota,
            RadarEnabled = plan.RadarEnabled,
            MaxDailySchedules = plan.MaxDailySchedules,
            IsActive = plan.IsActive,
            SortOrder = plan.SortOrder
        };
}
