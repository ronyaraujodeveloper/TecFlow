using Microsoft.EntityFrameworkCore;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Radar;
using TecFlow.Core.Entities;
using TecFlow.Database;
using TecFlow.Database.MultiTenancy;

namespace TecFlow.Infrastructure.Services.Radar;

public sealed class DealCreditsService : IDealCreditsService
{
    private readonly AppDbContext _context;
    private readonly ICurrentTenantService _currentTenant;
    private readonly ILiveCheckSearchService _liveCheck;

    public DealCreditsService(
        AppDbContext context,
        ICurrentTenantService currentTenant,
        ILiveCheckSearchService liveCheck)
    {
        _context = context;
        _currentTenant = currentTenant;
        _liveCheck = liveCheck;
    }

    public async Task<DealCreditsResponseDto> ListShowcaseAsync(
        int userId,
        LiveSearchFilterDto? search = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = await ResolveTenantIdAsync(userId, cancellationToken);
        if (tenantId is null)
        {
            return Fail("Conta sem tenant associado.");
        }

        var credits = await EnsureWalletAsync(tenantId.Value, cancellationToken);
        var unlocked = await _context.TenantDealUnlocks
            .AsNoTracking()
            .Where(item => item.TenantId == tenantId.Value)
            .Select(item => item.DealId)
            .ToListAsync(cancellationToken);
        var unlockedSet = unlocked.ToHashSet();
        var deals = await _context.GlobalTrendingDeals
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderByDescending(item => item.PriceDropPercent)
            .ThenByDescending(item => item.EngagementCount)
            .Take(40)
            .ToListAsync(cancellationToken);

        var items = deals.Select(item => MapDeal(item, unlockedSet.Contains(item.Id))).ToList();
        if (search is { HasAny: true })
        {
            items = items
                .Where(item => LiveSearchRules.Matches(
                    item.ProductName,
                    item.CurrentPrice,
                    null,
                    item.Platform,
                    null,
                    search.Keyword,
                    search.MinPrice,
                    search.MaxPrice,
                    search.HasCoupon,
                    search.Store))
                .ToList();
        }

        return new DealCreditsResponseDto
        {
            Credits = MapCredits(credits),
            Items = items
        };
    }

    public async Task<DealCreditsResponseDto> UnlockAsync(int userId, int dealId, CancellationToken cancellationToken = default)
    {
        var tenantId = await ResolveTenantIdAsync(userId, cancellationToken);
        if (tenantId is null)
        {
            return Fail("Conta sem tenant associado.");
        }

        var deal = await _context.GlobalTrendingDeals.FirstOrDefaultAsync(item => item.Id == dealId && item.IsActive, cancellationToken);
        if (deal is null)
        {
            return Fail("Achadinho não encontrado.");
        }

        var live = await _liveCheck.CheckUrlAsync(
            userId,
            deal.OriginalUrl,
            null,
            deal.CurrentPrice,
            cancellationToken);
        if (!live.IsAvailable)
        {
            deal.IsActive = false;
            deal.Touch();
            await _context.SaveChangesAsync(cancellationToken);
            return Fail("O anúncio não está mais ativo na loja.");
        }

        if (live.Price is > 0)
        {
            deal.PreviousPrice = deal.CurrentPrice;
            deal.CurrentPrice = live.Price.Value;
            deal.Touch();
        }

        var already = await _context.TenantDealUnlocks.AnyAsync(
            item => item.TenantId == tenantId.Value && item.DealId == dealId,
            cancellationToken);
        var wallet = await EnsureWalletAsync(tenantId.Value, cancellationToken);
        if (!already)
        {
            if (!DealCreditRules.TryConsume(wallet.DailyBalance, wallet.PurchasedBalance, DealCreditRules.UnlockCost, out var daily, out var purchased))
            {
                var empty = await ListShowcaseAsync(userId, cancellationToken: cancellationToken);
                empty.Status = false;
                empty.Descricao = "Saldo insuficiente. Compre um pacote de créditos.";
                return empty;
            }

            wallet.DailyBalance = daily;
            wallet.PurchasedBalance = purchased;
            wallet.Touch();
            _context.TenantDealUnlocks.Add(new TenantDealUnlock
            {
                TenantId = tenantId.Value,
                DealId = dealId,
                UnlockedAt = DateTime.UtcNow
            });
            _context.CreditTransactions.Add(new CreditTransaction
            {
                TenantId = tenantId.Value,
                Amount = -DealCreditRules.UnlockCost,
                Kind = DealCreditRules.KindUnlock,
                DealId = dealId
            });
            await _context.SaveChangesAsync(cancellationToken);
        }

        var result = await ListShowcaseAsync(userId, cancellationToken: cancellationToken);
        result.Descricao = already ? "Oferta já desbloqueada." : "Oferta desbloqueada com 1 crédito.";
        return result;
    }

    public async Task<DealCreditsResponseDto> TopUpAsync(int userId, int packSize, CancellationToken cancellationToken = default)
    {
        if (!DealCreditRules.IsValidPack(packSize))
        {
            return Fail("Pacote inválido. Use +10, +50 ou +100.");
        }

        var tenantId = await ResolveTenantIdAsync(userId, cancellationToken);
        if (tenantId is null)
        {
            return Fail("Conta sem tenant associado.");
        }

        var wallet = await EnsureWalletAsync(tenantId.Value, cancellationToken);
        wallet.PurchasedBalance += packSize;
        wallet.Touch();
        _context.CreditTransactions.Add(new CreditTransaction
        {
            TenantId = tenantId.Value,
            Amount = packSize,
            Kind = DealCreditRules.KindTopUp
        });
        await _context.SaveChangesAsync(cancellationToken);
        var result = await ListShowcaseAsync(userId, cancellationToken: cancellationToken);
        result.Descricao = $"Pacote de {packSize} créditos adicionado.";
        return result;
    }

    public async Task<int> GrantDailyQuotasAsync(CancellationToken cancellationToken = default)
    {
        var previousBypass = _currentTenant.BypassTenantFilters;
        _currentTenant.BypassTenantFilters = true;
        try
        {
            var now = DateTime.UtcNow;
            var users = await _context.UserAccounts
                .AsNoTracking()
                .Select(item => new { item.TenantId, item.Plan })
                .ToListAsync(cancellationToken);
            var granted = 0;
            foreach (var group in users.Where(item => item.TenantId != Guid.Empty).GroupBy(item => item.TenantId))
            {
                if (!group.Any(item => DealCreditRules.IsBasicPlan(item.Plan) || !string.IsNullOrWhiteSpace(item.Plan)))
                {
                    continue;
                }

                var wallet = await EnsureWalletAsync(group.Key, cancellationToken);
                if (!DealCreditRules.ShouldGrantDaily(wallet.DailyGrantedOnUtc, now))
                {
                    continue;
                }

                wallet.DailyBalance = DealCreditRules.DailyQuota;
                wallet.DailyGrantedOnUtc = now;
                wallet.Touch();
                _context.CreditTransactions.Add(new CreditTransaction
                {
                    TenantId = group.Key,
                    Amount = DealCreditRules.DailyQuota,
                    Kind = DealCreditRules.KindDailyGrant,
                    ExpiresAt = DealCreditRules.DailyExpiry(now)
                });
                granted++;
            }

            if (granted > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            return granted;
        }
        finally
        {
            _currentTenant.BypassTenantFilters = previousBypass;
        }
    }

    private async Task<Guid?> ResolveTenantIdAsync(int userId, CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            return null;
        }

        return await _context.UserAccounts
            .AsNoTracking()
            .Where(item => item.Id == userId)
            .Select(item => (Guid?)item.TenantId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<TenantCredit> EnsureWalletAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var wallet = await _context.TenantCredits.FirstOrDefaultAsync(item => item.TenantId == tenantId, cancellationToken);
        if (wallet is not null)
        {
            return wallet;
        }

        wallet = new TenantCredit { TenantId = tenantId };
        _context.TenantCredits.Add(wallet);
        await _context.SaveChangesAsync(cancellationToken);
        return wallet;
    }

    private static TenantCreditsDto MapCredits(TenantCredit wallet)
    {
        var now = DateTime.UtcNow;
        var daily = DealCreditRules.ShouldGrantDaily(wallet.DailyGrantedOnUtc, now) ? 0 : wallet.DailyBalance;
        return new TenantCreditsDto
        {
            DailyBalance = daily,
            PurchasedBalance = wallet.PurchasedBalance,
            TotalBalance = DealCreditRules.TotalBalance(daily, wallet.PurchasedBalance),
            DailyGrantedOnUtc = wallet.DailyGrantedOnUtc,
            DailyExpiresAtUtc = wallet.DailyGrantedOnUtc is null ? null : DealCreditRules.DailyExpiry(wallet.DailyGrantedOnUtc.Value)
        };
    }

    private static GlobalTrendingDealDto MapDeal(GlobalTrendingDeal item, bool unlocked) =>
        new()
        {
            Id = item.Id,
            Platform = item.Platform,
            PlatformProductId = item.PlatformProductId,
            ProductName = item.ProductName,
            ProductImageUrl = unlocked ? item.ProductImageUrl : item.ProductImageUrl,
            OriginalUrl = unlocked ? item.OriginalUrl : null,
            CurrentPrice = item.CurrentPrice,
            PreviousPrice = unlocked ? item.PreviousPrice : null,
            PriceDropPercent = item.PriceDropPercent,
            EngagementCount = item.EngagementCount,
            IsUnlocked = unlocked,
            IsSuperAchado = DealCreditRules.IsSuperAchado(item.PriceDropPercent, item.EngagementCount),
            LastSeenAt = item.LastSeenAt
        };

    private static DealCreditsResponseDto Fail(string message) =>
        new() { Status = false, Descricao = message };
}
