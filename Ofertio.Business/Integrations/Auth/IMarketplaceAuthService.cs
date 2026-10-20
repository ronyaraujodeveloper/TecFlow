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
}
