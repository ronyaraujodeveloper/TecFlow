using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TecFlow.Core.Abstractions;
using TecFlow.Core.Entities;

namespace TecFlow.Database.MultiTenancy;

internal static class TenantQueryFilterExtensions
{
    public static void ApplyTenantQueryFilters(this ModelBuilder modelBuilder, ICurrentTenantService tenant)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            if (clrType == typeof(Tenant))
            {
                continue;
            }

            if (typeof(ITenantScopedEntity).IsAssignableFrom(clrType))
            {
                var parameter = Expression.Parameter(clrType, "e");
                var tenantFilter = BuildTenantFilterExpression(parameter, tenant);

                Expression? combined = tenantFilter;

                if (typeof(IShopScopedEntity).IsAssignableFrom(clrType))
                {
                    var shopFilter = BuildShopFilterExpression(parameter, tenant);
                    combined = Expression.AndAlso(tenantFilter, shopFilter);
                }
                else if (clrType == typeof(Product))
                {
                    var shopFilter = BuildProductShopFilterExpression(parameter, tenant);
                    combined = Expression.AndAlso(tenantFilter, shopFilter);
                }

                var lambda = Expression.Lambda(combined, parameter);
                modelBuilder.Entity(clrType).HasQueryFilter(lambda);
            }
        }
    }

    private static Expression BuildTenantFilterExpression(ParameterExpression parameter, ICurrentTenantService tenant)
    {
        var entity = Expression.Convert(parameter, typeof(ITenantScopedEntity));
        var tenantIdProp = Expression.Property(entity, nameof(ITenantScopedEntity.TenantId));

        var bypass = Expression.Property(
            Expression.Constant(tenant),
            nameof(ICurrentTenantService.BypassTenantFilters));

        var currentTenantId = Expression.Property(
            Expression.Constant(tenant),
            nameof(ICurrentTenantService.TenantId));

        var tenantResolved = Expression.NotEqual(currentTenantId, Expression.Constant(null, typeof(Guid?)));
        var tenantMatch = Expression.Equal(
            tenantIdProp,
            Expression.Convert(currentTenantId, typeof(Guid)));

        var tenantBranch = Expression.AndAlso(tenantResolved, tenantMatch);
        return Expression.OrElse(bypass, tenantBranch);
    }

    private static Expression BuildShopFilterExpression(ParameterExpression parameter, ICurrentTenantService tenant)
    {
        var entity = Expression.Convert(parameter, typeof(IShopScopedEntity));
        var shopIdProp = Expression.Property(entity, nameof(IShopScopedEntity.ShopId));

        var currentShopId = Expression.Property(
            Expression.Constant(tenant),
            nameof(ICurrentTenantService.ShopId));

        var shopNotSelected = Expression.Equal(currentShopId, Expression.Constant(null, typeof(string)));
        var shopMatch = Expression.Equal(shopIdProp, currentShopId);

        return Expression.OrElse(shopNotSelected, shopMatch);
    }

    private static Expression BuildProductShopFilterExpression(ParameterExpression parameter, ICurrentTenantService tenant)
    {
        var marketplaceShopId = Expression.Property(parameter, nameof(Product.MarketplaceShopId));

        var currentShopId = Expression.Property(
            Expression.Constant(tenant),
            nameof(ICurrentTenantService.ShopId));

        var shopNotSelected = Expression.Equal(currentShopId, Expression.Constant(null, typeof(string)));
        var shopIsNull = Expression.Equal(marketplaceShopId, Expression.Constant(null, typeof(string)));
        var shopMatch = Expression.Equal(marketplaceShopId, currentShopId);
        var shopBranch = Expression.OrElse(shopIsNull, shopMatch);

        return Expression.OrElse(shopNotSelected, shopBranch);
    }
}
