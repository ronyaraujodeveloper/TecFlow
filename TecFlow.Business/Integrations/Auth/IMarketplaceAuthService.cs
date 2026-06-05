using TecFlow.Core.Enums;

namespace TecFlow.Business.Integrations.Auth;

public interface IMarketplaceAuthService
{
    string GenerateAuthorizationUrl(MarketplaceType type, string redirectUri, string? state = null);

    Task<MarketplaceTokenResult> CallbackAndGenerateTokensAsync(
        MarketplaceType type,
        string code,
        string shopId,
        CancellationToken cancellationToken = default);

    Task<string> GetValidTokenAsync(
        string shopId,
        MarketplaceType type,
        CancellationToken cancellationToken = default);
}
