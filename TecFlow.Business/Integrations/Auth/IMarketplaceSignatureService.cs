namespace TecFlow.Business.Integrations.Auth;

/// <summary>Gera assinaturas HMAC-SHA256 exigidas pelas APIs de produção Shopee e TikTok Shop.</summary>
public interface IMarketplaceSignatureService
{
    string GenerateShopeeSign(
        string partnerId,
        string partnerKey,
        string apiPath,
        long timestamp,
        string? accessToken = null,
        string? shopId = null);

    string GenerateTikTokShopSign(
        string appKey,
        string appSecret,
        string apiPath,
        long timestamp,
        string? requestBody = null);
}
