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

    public Task<MarketplaceAccount?> GetByShopAsync(string shopId, MarketplaceType marketplaceType)
    {
        var query = _context.MarketplaceAccounts
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(a => a.ShopId == shopId && a.MarketplaceType == marketplaceType);

        if (_currentTenant.TenantId is { } tenantId && tenantId != Guid.Empty)
        {
            query = query.Where(a => a.TenantId == tenantId);
        }

        return query.FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<MarketplaceAccount>> ListForCurrentTenantAsync(bool consolidatedAllShops = true)
    {
        var list = await _context.MarketplaceAccounts
            .AsNoTracking()
            .OrderBy(a => a.MarketplaceType)
            .ThenBy(a => a.ShopName)
            .ToListAsync();
        return list;
    }

    public async Task<IReadOnlyList<MarketplaceAccount>> ListForShopAsync(string shopId)
    {
        var list = await _context.MarketplaceAccounts
            .AsNoTracking()
            .WithManualTenantScope(_currentTenant)
            .Where(a => a.ShopId == shopId)
            .ToListAsync();
        return list;
    }

    public async Task<IReadOnlyList<MarketplaceAccount>> ListByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var key = userId?.Trim() ?? string.Empty;
        return await _context.MarketplaceAccounts
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(account => account.IsActive && account.UserId == key)
            .OrderByDescending(account => account.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<MarketplaceAccount?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.MarketplaceAccounts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(account => account.Id == id, cancellationToken);

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
            if (string.IsNullOrWhiteSpace(account.FriendlyName))
            {
                account.FriendlyName = string.IsNullOrWhiteSpace(account.ShopName)
                    ? account.ShopId
                    : account.ShopName;
            }

            account.IsActive = true;
            await _context.MarketplaceAccounts.AddAsync(account);
        }
        else
        {
            existing.ShopName = account.ShopName;
            existing.FriendlyName = string.IsNullOrWhiteSpace(account.FriendlyName)
                ? existing.FriendlyName
                : account.FriendlyName;
            existing.UserId = string.IsNullOrWhiteSpace(account.UserId) ? existing.UserId : account.UserId;
            existing.AffiliateTrackingId = string.IsNullOrWhiteSpace(account.AffiliateTrackingId)
                ? existing.AffiliateTrackingId
                : account.AffiliateTrackingId;
            existing.IsActive = account.IsActive;
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
