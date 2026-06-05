using Microsoft.EntityFrameworkCore;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database;
using TecFlow.Database.MultiTenancy;

namespace TecFlow.Infrastructure.Services.Repositories;

public class MarketplaceOrderRepository : IMarketplaceOrderRepository
{
    private readonly AppDbContext _context;
    private readonly ICurrentTenantService _currentTenant;

    public MarketplaceOrderRepository(AppDbContext context, ICurrentTenantService currentTenant)
    {
        _context = context;
        _currentTenant = currentTenant;
    }

    public Task<bool> ExistsAsync(string externalOrderId, string shopId, MarketplaceType marketplaceType) =>
        _context.MarketplaceOrders.AnyAsync(o =>
            o.ExternalOrderId == externalOrderId &&
            o.ShopId == shopId &&
            o.MarketplaceType == marketplaceType);

    public Task<MarketplaceOrder?> GetByExternalIdAsync(
        string externalOrderId,
        string shopId,
        MarketplaceType marketplaceType) =>
        _context.MarketplaceOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o =>
                o.ExternalOrderId == externalOrderId &&
                o.ShopId == shopId &&
                o.MarketplaceType == marketplaceType);

    public async Task<MarketplaceOrder> CreateAsync(MarketplaceOrder order)
    {
        order.CreatedAt = DateTime.UtcNow;
        order.ProcessedAt = DateTime.UtcNow;
        await _context.MarketplaceOrders.AddAsync(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<IReadOnlyList<MarketplaceOrder>> ListConsolidatedForCurrentTenantAsync()
    {
        return await _context.MarketplaceOrders
            .Include(o => o.Lines)
            .OrderByDescending(o => o.ProcessedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<MarketplaceOrder>> ListForShopAsync(string shopId)
    {
        return await _context.MarketplaceOrders
            .WithManualTenantScope(_currentTenant)
            .Where(o => o.ShopId == shopId)
            .Include(o => o.Lines)
            .OrderByDescending(o => o.ProcessedAt)
            .ToListAsync();
    }
}
