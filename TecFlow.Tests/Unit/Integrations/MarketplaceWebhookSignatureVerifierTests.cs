using TecFlow.Business.Integrations.Common;
using TecFlow.Infrastructure.Services.Integrations.Webhooks;
using TecFlow.Tests.Helpers;

namespace TecFlow.Tests.Unit.Integrations;

public class MarketplaceWebhookSignatureVerifierTests
{
    private readonly MarketplaceWebhookSignatureVerifier _verifier;

    public MarketplaceWebhookSignatureVerifierTests()
    {
        _verifier = new MarketplaceWebhookSignatureVerifier(
            MarketplaceTestOptionsFactory.ShopeeOptions(),
            MarketplaceTestOptionsFactory.TikTokOptions());
    }

    [Fact]
    public void VerifyShopeePush_ShouldReturnTrue_WhenAuthorizationMatchesBody()
    {
        // Arrange
        const string body = """{"code":3,"shop_id":1,"data":{"ordersn":"ORD-1","status":"READY_TO_SHIP"}}""";
        var signature = MarketplaceSignatureHelper.GenerateShopeeWebhookSign(
            MarketplaceTestOptionsFactory.ShopeePartnerKey,
            MarketplaceTestOptionsFactory.WebhookCallbackUrl,
            body);

        // Act
        var valid = _verifier.VerifyShopeePush(body, signature);

        // Assert
        Assert.True(valid);
    }

    [Fact]
    public void VerifyShopeePush_ShouldReturnFalse_WhenAuthorizationIsInvalid()
    {
        // Arrange
        const string body = """{"code":3}""";

        // Act
        var valid = _verifier.VerifyShopeePush(body, "deadbeef");

        // Assert
        Assert.False(valid);
    }

    [Fact]
    public void VerifyShopeePush_ShouldReturnFalse_WhenAuthorizationHeaderIsMissing()
    {
        // Act
        var valid = _verifier.VerifyShopeePush("{}", null);

        // Assert
        Assert.False(valid);
    }

    [Fact]
    public void VerifyTikTokShopPush_ShouldReturnTrue_WhenHexSignatureMatchesAppSecret()
    {
        // Arrange
        const string body = """{"type":"order_status_change","data":{"order_id":"99"}}""";
        var signature = MarketplaceSignatureHelper.GenerateTikTokShopWebhookSign(
            MarketplaceTestOptionsFactory.TikTokAppSecret,
            body);

        // Act
        var valid = _verifier.VerifyTikTokShopPush(body, signature);

        // Assert
        Assert.True(valid);
    }

    [Fact]
    public void VerifyTikTokShopPush_ShouldReturnFalse_WhenSignatureIsCorrupted()
    {
        // Arrange
        const string body = """{"type":"order_status_change"}""";

        // Act
        var valid = _verifier.VerifyTikTokShopPush(body, "00".PadRight(64, '0'));

        // Assert
        Assert.False(valid);
    }

    [Fact]
    public void VerifyTikTokShopPush_ShouldReturnFalse_WhenTimestampIsExpired()
    {
        // Arrange
        const string body = "{}";
        var oldTimestamp = DateTimeOffset.UtcNow.AddHours(-2).ToUnixTimeSeconds();
        var signedPayload = $"{oldTimestamp}.{body}";
        var signature = MarketplaceSignatureHelper.GenerateTikTokShopWebhookSign(
            MarketplaceTestOptionsFactory.TikTokAppSecret,
            signedPayload);
        var header = $"t={oldTimestamp},s={signature}";

        // Act
        var valid = _verifier.VerifyTikTokShopPush(body, header);

        // Assert
        Assert.False(valid);
    }
}
