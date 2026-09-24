using Microsoft.EntityFrameworkCore;
using TecFlow.Business.Integrations;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database;
using TecFlow.Database.MultiTenancy;

namespace TecFlow.Infrastructure.Services.Repositories;

    public class MarketplaceAccountRepository : IMarketplaceAccountRepository
    {
        private readonly AppDbContext _context;
        private readonly ICurrentTenantService _currentTenant;
        private readonly ITenantProvisioningService _tenantProvisioning;

        public MarketplaceAccountRepository(
            AppDbContext context,
            ICurrentTenantService currentTenant,
            ITenantProvisioningService tenantProvisioning)
        {
            _context = context;
            _currentTenant = currentTenant;
            _tenantProvisioning = tenantProvisioning;
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
            .Where(account => account.UserId == key
                && (account.IsActive
                    || (account.TrackingId != null && account.TrackingId.Contains("http"))
                    || (account.AffiliateTrackingId != null && account.AffiliateTrackingId.Contains("http"))))
            .OrderByDescending(account => account.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task SanitizeHttpTrackingIdsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var key = userId?.Trim() ?? string.Empty;
        var accounts = await _context.MarketplaceAccounts
            .IgnoreQueryFilters()
            .Where(account => account.UserId == key
                && ((account.TrackingId != null && account.TrackingId.Contains("http"))
                    || (account.AffiliateTrackingId != null && account.AffiliateTrackingId.Contains("http"))))
            .ToListAsync(cancellationToken);

        if (accounts.Count == 0)
        {
            return;
        }

        var changed = false;
        foreach (var account in accounts)
        {
            var raw = string.IsNullOrWhiteSpace(account.TrackingId)
                ? account.AffiliateTrackingId
                : account.TrackingId;
            if (AffiliateTrackingIdValidator.TryNormalize(account.MarketplaceType, raw, out var cleanId)
                && !string.IsNullOrWhiteSpace(cleanId))
            {
                account.TrackingId = cleanId;
                account.AffiliateTrackingId = cleanId;
                account.IsActive = true;
            }
            else
            {
                account.IsActive = false;
            }

            account.Touch();
            changed = true;
        }

        if (changed)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public Task<bool> ExistsActiveTrackingIdAsync(
        MarketplaceType marketplaceType,
        string trackingId,
        int? excludeAccountId = null,
        CancellationToken cancellationToken = default)
    {
        var id = trackingId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(id))
        {
            return Task.FromResult(false);
        }

        var query = _context.MarketplaceAccounts
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(account => account.IsActive
                && account.MarketplaceType == marketplaceType
                && (account.TrackingId == id || account.AffiliateTrackingId == id));

        if (excludeAccountId is > 0)
        {
            query = query.Where(account => account.Id != excludeAccountId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<MarketplaceAccount?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.MarketplaceAccounts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(account => account.Id == id, cancellationToken);

    public async Task<bool> SetInactiveByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var account = await _context.MarketplaceAccounts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (account is null)
        {
            Console.WriteLine($"[MarketplaceAccountRepository] SetInactiveByIdAsync: conta {id} não encontrada.");
            return false;
        }

        account.IsActive = false;
        account.Touch();
        await _context.SaveChangesAsync(cancellationToken);
        Console.WriteLine($"[MarketplaceAccountRepository] Conta {id} inativada (IsActive=false).");
        return true;
    }

    public async Task UpsertAsync(MarketplaceAccount account)
    {
        var tenant = await _tenantProvisioning.EnsurePersistedTenantAsync(
            account.TenantId == Guid.Empty ? (Guid?)null : account.TenantId);
        account.TenantId = tenant.Id;

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

        await _context.MarketplaceAccounts.AddAsync(account);
        await _context.SaveChangesAsync();
        }
        else
        {
            existing.ShopName = account.ShopName;
            existing.FriendlyName = string.IsNullOrWhiteSpace(account.FriendlyName)
                ? existing.FriendlyName
                : account.FriendlyName;
            existing.UserId = string.IsNullOrWhiteSpace(account.UserId) ? existing.UserId : account.UserId;
            existing.TrackingId = string.IsNullOrWhiteSpace(account.TrackingId)
                ? existing.TrackingId
                : account.TrackingId;
            existing.AffiliateTrackingId = string.IsNullOrWhiteSpace(account.AffiliateTrackingId)
                ? existing.AffiliateTrackingId
                : account.AffiliateTrackingId;
            existing.AppKey = string.IsNullOrWhiteSpace(account.AppKey) ? existing.AppKey : account.AppKey;
            existing.AppSecret = string.IsNullOrWhiteSpace(account.AppSecret) ? existing.AppSecret : account.AppSecret;
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
