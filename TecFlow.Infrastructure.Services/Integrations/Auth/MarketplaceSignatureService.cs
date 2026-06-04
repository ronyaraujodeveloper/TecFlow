using TecFlow.Business.Integrations.Auth;
using TecFlow.Business.Integrations.Common;

namespace TecFlow.Infrastructure.Services.Integrations.Auth;

public class MarketplaceSignatureService : IMarketplaceSignatureService
{
    public string GenerateShopeeSign(
        string partnerId,
        string partnerKey,
        string apiPath,
        long timestamp,
        string? accessToken = null,
        string? shopId = null) =>
        MarketplaceSignatureHelper.GenerateShopeeSign(
            partnerId, partnerKey, apiPath, timestamp, accessToken, shopId);

    public string GenerateTikTokShopSign(
        string appKey,
        string appSecret,
        string apiPath,
        long timestamp,
        string? requestBody = null) =>
        MarketplaceSignatureHelper.GenerateTikTokShopSign(
            appKey, appSecret, apiPath, timestamp, requestBody);
}
