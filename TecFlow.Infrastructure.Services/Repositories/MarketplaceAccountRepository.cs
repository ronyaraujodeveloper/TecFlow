using Microsoft.EntityFrameworkCore;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database;
using TecFlow.Database.MultiTenancy;

namespace TecFlow.Infrastructure.Services.Repositories;

public class MarketplaceAccountRepository : IMarketplaceAccountRepository
{
    private readonly AppDbContext _context;
    private readonly ICurrentTenantService _currentTenant;

    public MarketplaceAccountRepository(AppDbContext context, ICurrentTenantService currentTenant)
    {
        _context = context;
        _currentTenant = currentTenant;
    }

    public Task<MarketplaceAccount?> GetByShopAsync(string shopId, MarketplaceType marketplaceType) =>
        _context.MarketplaceAccounts.FirstOrDefaultAsync(a =>
            a.ShopId == shopId && a.MarketplaceType == marketplaceType);

    public async Task<IReadOnlyList<MarketplaceAccount>> ListForCurrentTenantAsync(bool consolidatedAllShops = true)
    {
        var list = await _context.MarketplaceAccounts
            .OrderBy(a => a.MarketplaceType)
            .ThenBy(a => a.ShopName)
            .ToListAsync();
        return list;
    }

    public async Task<IReadOnlyList<MarketplaceAccount>> ListForShopAsync(string shopId)
    {
        var list = await _context.MarketplaceAccounts
            .WithManualTenantScope(_currentTenant)
            .Where(a => a.ShopId == shopId)
            .ToListAsync();
        return list;
    }

    public async Task UpsertAsync(MarketplaceAccount account)
    {
        var existing = await _context.MarketplaceAccounts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a =>
                a.TenantId == account.TenantId &&
                a.ShopId == account.ShopId &&
                a.MarketplaceType == account.MarketplaceType);

        if (existing is null)
        {
            account.CreatedAt = DateTime.UtcNow;
            await _context.MarketplaceAccounts.AddAsync(account);
        }
        else
        {
            existing.ShopName = account.ShopName;
            existing.AccessToken = account.AccessToken;
            existing.RefreshToken = account.RefreshToken;
            existing.ExpiresAt = account.ExpiresAt;
            existing.RefreshExpiresAt = account.RefreshExpiresAt;
            existing.Cnpj = account.Cnpj;
            existing.Touch();
            _context.MarketplaceAccounts.Update(existing);
        }

        await _context.SaveChangesAsync();
    }
}
