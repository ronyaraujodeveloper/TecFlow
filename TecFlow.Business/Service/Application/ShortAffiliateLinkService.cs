using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database.Entity;

namespace TecFlow.Business.Service.Application;

/// <summary>
/// Cruzamento de contas MarketplaceAccounts e substituição das tags de afiliado Magalu
/// após a descompactação de agregadores (ofertou.ai).
/// </summary>
public static class ShortAffiliateLinkService
{
    public const string MagaluPromoterQuery = "promoter_id";

    public static bool IsSamePlatform(MarketplaceType left, MarketplaceType right) =>
        left.AreSamePlatform(right);

    public static bool IsSamePlatform(MarketplaceType platform, string? alias) =>
        platform.AreSamePlatform(alias);

    public static MarketplaceAccount? FindActiveAccount(
        IEnumerable<MarketplaceAccount> accounts,
        MarketplaceType expectedPlatform)
    {
        return accounts
            .Where(account => account.IsActive && MatchesPlatform(account, expectedPlatform))
            .OrderByDescending(HasAffiliateCredential)
            .ThenByDescending(account => account.CreatedAt)
            .FirstOrDefault();
    }

    public static IntegracaoLoja? FindActiveStore(
        IEnumerable<IntegracaoLoja> stores,
        MarketplaceType expectedPlatform)
    {
        return stores
            .Where(store =>
                store.Status != MarketplaceIntegrationStatus.Inactive
                && store.PlatformType.AreSamePlatform(expectedPlatform))
            .OrderByDescending(store => !string.IsNullOrWhiteSpace(store.AffiliateTrackingId))
            .ThenByDescending(store => store.CreatedAt)
            .FirstOrDefault();
    }

    public static bool MatchesPlatform(MarketplaceAccount account, MarketplaceType expectedPlatform) =>
        account.MarketplaceType.AreSamePlatform(expectedPlatform)
        || expectedPlatform.AreSamePlatform(account.MarketplaceType.ToString())
        || expectedPlatform.AreSamePlatform(account.MarketplaceType.GetDisplayName())
        || expectedPlatform.AreSamePlatform(account.FriendlyName)
        || expectedPlatform.AreSamePlatform(account.ShopName);

    public static string ResolveMagaluTrackingId(MarketplaceAccount? account, IntegracaoLoja? store)
    {
        if (!string.IsNullOrWhiteSpace(account?.TrackingId))
        {
            return account.TrackingId.Trim();
        }

        if (!string.IsNullOrWhiteSpace(account?.AffiliateTrackingId))
        {
            return account.AffiliateTrackingId.Trim();
        }

        if (!string.IsNullOrWhiteSpace(store?.AffiliateTrackingId))
        {
            return store.AffiliateTrackingId.Trim();
        }

        return string.Empty;
    }

    private static bool HasAffiliateCredential(MarketplaceAccount account) =>
        !string.IsNullOrWhiteSpace(account.TrackingId)
        || !string.IsNullOrWhiteSpace(account.AffiliateTrackingId);
}
