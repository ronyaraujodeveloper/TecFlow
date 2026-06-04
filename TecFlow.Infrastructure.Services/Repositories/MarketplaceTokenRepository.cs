using Microsoft.EntityFrameworkCore;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Repositories;

public class MarketplaceTokenRepository : IMarketplaceTokenRepository
{
    private readonly AppDbContext _context;

    public MarketplaceTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<MarketplaceToken?> GetByShopAndMarketplaceAsync(
        string shopId,
        MarketplaceType marketplaceType) =>
        await _context.MarketplaceTokens
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
}
