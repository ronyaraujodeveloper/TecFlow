namespace Ofertio.Business.Integrations.Webhooks;

public interface IMarketplaceWebhookSignatureVerifier
{
    bool VerifyShopeePush(string rawBody, string? authorizationHeader);

    bool VerifyTikTokShopPush(string rawBody, string? signatureHeader);
}
