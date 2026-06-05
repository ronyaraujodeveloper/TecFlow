using Microsoft.EntityFrameworkCore;
using TecFlow.Core.Abstractions;

namespace TecFlow.Database.MultiTenancy;

/// <summary>Extensões para consultas consolidadas (todas as lojas) ou loja específica.</summary>
public static class TenantDbSetExtensions
{
    /// <summary>Visão consolidada: filtros globais ativos; ShopId no contexto deve ser null.</summary>
    public static IQueryable<T> ForTenantConsolidated<T>(this IQueryable<T> query)
        where T : class => query;

    public static IQueryable<T> WithoutTenantFilters<T>(this IQueryable<T> query)
        where T : class =>
        query.IgnoreQueryFilters();

    public static IQueryable<T> WithManualTenantScope<T>(
        this IQueryable<T> query,
        ICurrentTenantService tenant)
        where T : class, ITenantScopedEntity
    {
        var scoped = query.IgnoreQueryFilters();
        if (tenant.BypassTenantFilters || tenant.TenantId is null)
        {
            return scoped;
        }

        return scoped.Where(e => e.TenantId == tenant.TenantId);
    }
}
