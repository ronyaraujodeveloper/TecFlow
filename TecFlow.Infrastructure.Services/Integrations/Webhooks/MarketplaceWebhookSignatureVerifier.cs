using Microsoft.Extensions.Options;
using TecFlow.Business.Integrations.Common;
using TecFlow.Business.Integrations.Shopee;
using TecFlow.Business.Integrations.TikTokShop;
using TecFlow.Business.Integrations.Webhooks;

namespace TecFlow.Infrastructure.Services.Integrations.Webhooks;

public class MarketplaceWebhookSignatureVerifier : IMarketplaceWebhookSignatureVerifier
{
    private readonly ShopeeIntegrationOptions _shopeeOptions;
    private readonly TikTokShopIntegrationOptions _tikTokOptions;

    public MarketplaceWebhookSignatureVerifier(
        IOptions<ShopeeIntegrationOptions> shopeeOptions,
        IOptions<TikTokShopIntegrationOptions> tikTokOptions)
    {
        _shopeeOptions = shopeeOptions.Value;
        _tikTokOptions = tikTokOptions.Value;
    }

    public bool VerifyShopeePush(string rawBody, string? authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader) ||
            string.IsNullOrWhiteSpace(_shopeeOptions.PartnerKey) ||
            string.IsNullOrWhiteSpace(_shopeeOptions.WebhookCallbackUrl))
        {
            return false;
        }

        var expected = MarketplaceSignatureHelper.GenerateShopeeWebhookSign(
            _shopeeOptions.PartnerKey,
            _shopeeOptions.WebhookCallbackUrl.Trim(),
            rawBody);

        return MarketplaceSignatureHelper.CompareHexSignatures(expected, authorizationHeader);
    }

    public bool VerifyTikTokShopPush(string rawBody, string? signatureHeader)
    {
        var secret = !string.IsNullOrWhiteSpace(_tikTokOptions.WebhookSecret)
            ? _tikTokOptions.WebhookSecret!
            : _tikTokOptions.AppSecret;

        if (string.IsNullOrWhiteSpace(signatureHeader) || string.IsNullOrWhiteSpace(secret))
        {
            return false;
        }

        if (signatureHeader.Contains("t=", StringComparison.Ordinal) &&
            signatureHeader.Contains("s=", StringComparison.Ordinal))
        {
            return VerifyTikTokSignedHeader(rawBody, signatureHeader, secret);
        }

        return MarketplaceSignatureHelper.VerifyHmacSha256Hex(secret, rawBody, signatureHeader);
    }

    private static bool VerifyTikTokSignedHeader(string rawBody, string signatureHeader, string secret)
    {
        var parts = signatureHeader.Split(',', StringSplitOptions.TrimEntries);
        string? timestamp = null;
        string? signature = null;

        foreach (var part in parts)
        {
            var idx = part.IndexOf('=');
            if (idx <= 0)
            {
                continue;
            }

            var prefix = part[..idx];
            var value = part[(idx + 1)..];
            if (prefix == "t")
            {
                timestamp = value;
            }
            else if (prefix == "s")
            {
                signature = value;
            }
        }

        if (string.IsNullOrWhiteSpace(timestamp) || string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        if (long.TryParse(timestamp, out var ts))
        {
            var age = Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - ts);
            if (age > 300)
            {
                return false;
            }
        }

        var signedPayload = $"{timestamp}.{rawBody}";
        return MarketplaceSignatureHelper.VerifyHmacSha256Hex(secret, signedPayload, signature);
    }
}
