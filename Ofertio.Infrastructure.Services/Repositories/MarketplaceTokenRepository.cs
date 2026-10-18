using Microsoft.EntityFrameworkCore;
using Ofertio.Business.Interfaces.Repositories;
using Ofertio.Core.Entities;
using Ofertio.Core.Enums;
using Ofertio.Database;
using Ofertio.Database.MultiTenancy;

namespace Ofertio.Infrastructure.Services.Repositories;

public class MarketplaceTokenRepository : IMarketplaceTokenRepository
{
    private readonly AppDbContext _context;
    private readonly ICurrentTenantService _currentTenant;

    public MarketplaceTokenRepository(AppDbContext context, ICurrentTenantService currentTenant)
    {
        _context = context;
        _currentTenant = currentTenant;
    }

    public async Task<MarketplaceToken?> GetByShopAndMarketplaceAsync(
        string shopId,
        MarketplaceType marketplaceType) =>
        await _context.MarketplaceTokens
            .FirstOrDefaultAsync(t =>
                t.ShopId == shopId && t.MarketplaceType == marketplaceType);

    public Task<MarketplaceToken?> GetByShopAndMarketplaceIgnoreTenantAsync(
        string shopId,
        MarketplaceType marketplaceType) =>
        _context.MarketplaceTokens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t =>
                t.ShopId == shopId && t.MarketplaceType == marketplaceType);

    public async Task UpsertAsync(MarketplaceToken token)
    {
        var existing = await GetByShopAndMarketplaceAsync(token.ShopId, token.MarketplaceType);

        if (existing is null)
        {
            token.CreatedAt = DateTime.UtcNow;
            await _context.MarketplaceTokens.AddAsync(token);
        }
        else
        {
            existing.AccessToken = token.AccessToken;
            existing.RefreshToken = token.RefreshToken;
            existing.ExpiresAt = token.ExpiresAt;
            existing.RefreshExpiresAt = token.RefreshExpiresAt;
            existing.Touch();
            _context.MarketplaceTokens.Update(existing);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<MarketplaceToken>> ListConsolidatedForCurrentTenantAsync()
    {
        return await _context.MarketplaceTokens
            .OrderBy(t => t.MarketplaceType)
            .ThenBy(t => t.ShopId)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<MarketplaceToken>> ListForShopAsync(string shopId)
    {
        return await _context.MarketplaceTokens
            .WithManualTenantScope(_currentTenant)
            .Where(t => t.ShopId == shopId)
            .ToListAsync();
    }
}
