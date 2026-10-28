using Ofertio.Core.Enums;

namespace Ofertio.Business.Integrations.Auth;

public interface IMarketplaceAuthService
{
    string GenerateAuthorizationUrl(MarketplaceType type, string redirectUri, string? state = null);

    Task<MarketplaceTokenResult> CallbackAndGenerateTokensAsync(
        MarketplaceType type,
        string code,
        string shopId,
        CancellationToken cancellationToken = default,
        string? userId = null);

    Task<string> GetValidTokenAsync(
        string shopId,
        MarketplaceType type,
        CancellationToken cancellationToken = default);

    /// <summary>Troca o authorization code do Mercado Livre por access/refresh token (sem persistir).</summary>
    Task<MarketplaceOAuthExchangeResult> ExchangeMercadoLivreCodeAsync(
        string code,
        string? redirectUri,
        string? codeVerifier = null,
        CancellationToken cancellationToken = default);

    /// <summary>Troca o <c>auth_code</c> do TikTok Shop por access/refresh token, <c>open_id</c> e <c>user_type</c> (sem persistir).</summary>
    Task<MarketplaceOAuthExchangeResult> ExchangeTikTokShopCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    /// <summary>Renova um par de tokens do TikTok Shop (sem persistir).</summary>
    Task<MarketplaceOAuthExchangeResult> RefreshTikTokShopTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);
}
