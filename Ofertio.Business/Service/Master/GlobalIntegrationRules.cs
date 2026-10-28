using Ofertio.Business.Dto;
using Ofertio.Business.Integrations.Auth;
using Ofertio.Core.Entities;

namespace Ofertio.Business.Service.Master;

/// <summary>Regras das conexões Master com APIs oficiais (status do token e catálogo de provedores).</summary>
public static class GlobalIntegrationRules
{
    public const string StatusActive = "Ativo";
    public const string StatusExpired = "Expirado";
    public const string StatusDisconnected = "Desconectado";

    public const string OverviewPath = "/master/integracoes-globais";
    public const string MercadoLivrePath = MarketplaceOAuthStateRules.MasterMercadoLivrePath;
    public const string TikTokShopPath = MarketplaceOAuthStateRules.MasterTikTokShopPath;

    public const string MercadoLivreKey = "mercado-livre";
    public const string TikTokShopKey = "tiktok-shop";

    /// <summary>Folga antes do vencimento em que o access token já é tratado como expirado.</summary>
    public static readonly TimeSpan FreshnessMargin = TimeSpan.FromMinutes(2);

    public static readonly IReadOnlyList<GlobalIntegrationProviderDto> Providers =
    [
        new(MercadoLivreKey, "API Mercado Livre", "bi-bag-check", MercadoLivrePath, true),
        new(TikTokShopKey, "API TikTok Shop", "bi-tiktok", TikTokShopPath, true),
        new("shopee", "API Shopee", "bi-shop", null, false)
    ];

    public static bool IsAccessTokenFresh(GlobalIntegrationToken? token, DateTime utcNow) =>
        token is not null
        && !string.IsNullOrWhiteSpace(token.AccessToken)
        && token.ExpiresAt > utcNow.Add(FreshnessMargin);

    public static bool CanRefresh(GlobalIntegrationToken? token, DateTime utcNow) =>
        token is not null
        && !string.IsNullOrWhiteSpace(token.RefreshToken)
        && (token.RefreshExpiresAt is null || token.RefreshExpiresAt > utcNow);

    /// <summary>
    /// Ativo: access token válido, ou vencido com refresh_token renovável e sem falha registrada.
    /// Expirado: vencido sem renovação possível ou última renovação falhou. Desconectado: sem token.
    /// </summary>
    public static string ResolveStatus(GlobalIntegrationToken? token, DateTime utcNow)
    {
        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
        {
            return StatusDisconnected;
        }

        if (!string.IsNullOrWhiteSpace(token.LastError))
        {
            return StatusExpired;
        }

        return IsAccessTokenFresh(token, utcNow) || CanRefresh(token, utcNow)
            ? StatusActive
            : StatusExpired;
    }

    public static string StatusBadgeClass(string? status) => status switch
    {
        StatusActive => "text-bg-success",
        StatusExpired => "text-bg-warning",
        _ => "text-bg-secondary"
    };

    public static string TruncateError(string? error) =>
        string.IsNullOrWhiteSpace(error) ? string.Empty : error.Length > 500 ? error[..500] : error.Trim();
}
