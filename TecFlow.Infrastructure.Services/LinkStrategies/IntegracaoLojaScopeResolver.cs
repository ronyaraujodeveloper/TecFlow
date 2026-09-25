using System.Globalization;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database.Entity;

namespace TecFlow.Infrastructure.Services.LinkStrategies;

/// <summary>Resolve IntegracaoLoja a partir do escopo global (Guid) e valida tokens.</summary>
public sealed class IntegracaoLojaScopeResolver : IIntegracaoLojaScopeResolver
{
    private readonly IIntegracaoLojaRepository _integracaoLojaRepository;
    private readonly IMarketplaceAccountRepository _marketplaceAccountRepository;
    private readonly ILogger<IntegracaoLojaScopeResolver> _logger;

    public IntegracaoLojaScopeResolver(
        IIntegracaoLojaRepository integracaoLojaRepository,
        IMarketplaceAccountRepository marketplaceAccountRepository,
        ILogger<IntegracaoLojaScopeResolver> logger)
    {
        _integracaoLojaRepository = integracaoLojaRepository;
        _marketplaceAccountRepository = marketplaceAccountRepository;
        _logger = logger;
    }

    public static string MissingConnectedAccountMessage(MarketplaceType platform) =>
        $"⚠️ Você ainda não tem uma conta da {platform.GetDisplayName()} conectada.";

    public async Task<IntegracaoLoja> ResolveAsync(
        Guid storeScopeId,
        int userId,
        MarketplaceType expectedPlatform,
        CancellationToken cancellationToken = default)
    {
        var decodedId = IntegracaoLojaScopeHelper.TryDecodeStoreScope(storeScopeId);
        var store = await TryResolveExplicitAsync(decodedId, userId, expectedPlatform, cancellationToken);

        if (store is null || store.PlatformType != expectedPlatform)
        {
            store = await TryResolveFirstActiveForPlatformAsync(userId, expectedPlatform, cancellationToken);
        }

        if (store is null)
        {
            _logger.LogWarning(
                "Nenhuma MarketplaceAccount ativa para a plataforma. StoreScope={StoreScopeId} DecodedId={DecodedId} UserId={UserId} Platform={Platform}",
                storeScopeId,
                decodedId,
                userId,
                expectedPlatform);

            throw new AffiliateLinkGenerationException(MissingConnectedAccountMessage(expectedPlatform));
        }

        _logger.LogInformation(
            "IntegracaoLoja resolvida no AppDbContext (AutomacaoSociais). StoreId={StoreId} UserId={UserId} TenantId={TenantId} ShopId={ShopId} Platform={Platform}",
            store.Id,
            store.UserId,
            store.TenantId,
            store.ShopId,
            store.PlatformType);

        if (store.Status == MarketplaceIntegrationStatus.Inactive)
        {
            throw new AffiliateLinkGenerationException(
                "A loja selecionada está inativa. Reconecte-a em Minhas Lojas / Integrações.");
        }

        if (store.PlatformType.IsUniversalAffiliatePlatform())
        {
            return store;
        }

        if (store.Status == MarketplaceIntegrationStatus.Expired || store.ExpiresAt <= DateTime.UtcNow)
        {
            throw new AffiliateLinkGenerationException(
                "O token de acesso da loja expirou. Reconecte a conta em Minhas Lojas / Integrações.");
        }

        if (string.IsNullOrWhiteSpace(store.AccessToken))
        {
            throw new AffiliateLinkGenerationException(
                "Token de acesso da loja indisponível. Reconecte a integração em Minhas Lojas / Integrações.");
        }

        return store;
    }

    private async Task<IntegracaoLoja?> TryResolveExplicitAsync(
        int? decodedId,
        int userId,
        MarketplaceType expectedPlatform,
        CancellationToken cancellationToken)
    {
        if (decodedId is not int id || id <= 0)
        {
            return null;
        }

        var store = await _integracaoLojaRepository.GetByIdAsync(id, cancellationToken);
        if (store is not null && store.PlatformType == expectedPlatform && UserOwnsStore(store, userId))
        {
            return store;
        }

        var account = await _marketplaceAccountRepository.GetByIdAsync(id, cancellationToken);
        if (account is null || account.MarketplaceType != expectedPlatform || !UserOwnsAccount(account, userId))
        {
            return null;
        }

        return await ResolveStoreForAccountAsync(account, userId, cancellationToken);
    }

    private async Task<IntegracaoLoja?> TryResolveFirstActiveForPlatformAsync(
        int userId,
        MarketplaceType expectedPlatform,
        CancellationToken cancellationToken)
    {
        var userKey = userId.ToString(CultureInfo.InvariantCulture);
        var accounts = await _marketplaceAccountRepository.ListByUserIdAsync(userKey, cancellationToken);
        var account = accounts
            .Where(item => item.IsActive && item.MarketplaceType == expectedPlatform)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefault();

        if (account is not null)
        {
            return await ResolveStoreForAccountAsync(account, userId, cancellationToken);
        }

        var lojas = await _integracaoLojaRepository.ListByUserIdAsync(userId, cancellationToken);
        return lojas
            .Where(item => item.PlatformType == expectedPlatform && item.Status != MarketplaceIntegrationStatus.Inactive)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefault();
    }

    private async Task<IntegracaoLoja> ResolveStoreForAccountAsync(
        MarketplaceAccount account,
        int userId,
        CancellationToken cancellationToken)
    {
        IntegracaoLoja? store = null;
        if (!string.IsNullOrWhiteSpace(account.ShopId))
        {
            store = await _integracaoLojaRepository.GetByUserShopPlatformAsync(
                userId,
                account.ShopId,
                account.MarketplaceType,
                cancellationToken);
        }

        if (store is null)
        {
            var lojas = await _integracaoLojaRepository.ListByUserIdAsync(userId, cancellationToken);
            store = lojas.FirstOrDefault(item =>
                item.PlatformType == account.MarketplaceType
                && string.Equals(item.ShopId, account.ShopId, StringComparison.OrdinalIgnoreCase));
        }

        return store ?? FromMarketplaceAccount(account, userId);
    }

    private static bool UserOwnsStore(IntegracaoLoja store, int userId) =>
        store.UserId == userId;

    private static bool UserOwnsAccount(MarketplaceAccount account, int userId)
    {
        if (string.IsNullOrWhiteSpace(account.UserId))
        {
            return true;
        }

        return string.Equals(
            account.UserId.Trim(),
            userId.ToString(CultureInfo.InvariantCulture),
            StringComparison.Ordinal);
    }

    private static IntegracaoLoja FromMarketplaceAccount(MarketplaceAccount account, int userId)
    {
        _ = int.TryParse(account.UserId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedUserId);
        var expiresAt = account.ExpiresAt == default ? DateTime.UtcNow.AddYears(1) : account.ExpiresAt;
        return new IntegracaoLoja
        {
            Id = account.Id,
            UserId = parsedUserId > 0 ? parsedUserId : userId,
            TenantId = account.TenantId,
            PlatformType = account.MarketplaceType,
            ShopId = account.ShopId,
            FriendlyName = string.IsNullOrWhiteSpace(account.FriendlyName) ? account.ShopName : account.FriendlyName,
            AffiliateTrackingId = string.IsNullOrWhiteSpace(account.TrackingId)
                ? account.AffiliateTrackingId
                : account.TrackingId,
            AccessToken = account.AccessToken,
            RefreshToken = account.RefreshToken,
            ExpiresAt = expiresAt,
            Status = account.IsActive
                ? (expiresAt <= DateTime.UtcNow
                    ? MarketplaceIntegrationStatus.Expired
                    : MarketplaceIntegrationStatus.Active)
                : MarketplaceIntegrationStatus.Inactive,
            CreatedAt = account.CreatedAt == default ? DateTime.UtcNow : account.CreatedAt
        };
    }
}
