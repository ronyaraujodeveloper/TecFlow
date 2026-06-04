using TecFlow.Business.Integrations.Common;

namespace TecFlow.Tests.Unit.Integrations;

public class MarketplaceSignatureHelperTests
{
    [Fact]
    public void GenerateShopeeSign_ShouldBeDeterministic_WhenInputsAreEqual()
    {
        // Arrange
        const string partnerId = "1";
        const string partnerKey = "secret";
        const string path = "/api/v2/product/get_item_list";
        const long timestamp = 1700000000;

        // Act
        var first = MarketplaceSignatureHelper.GenerateShopeeSign(partnerId, partnerKey, path, timestamp, "token", "shop");
        var second = MarketplaceSignatureHelper.GenerateShopeeSign(partnerId, partnerKey, path, timestamp, "token", "shop");

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(64, first.Length);
    }

    [Fact]
    public void GenerateShopeeWebhookSign_ShouldMatchCompareHex_WhenHeaderIsValid()
    {
        // Arrange
        const string partnerKey = "webhook-secret";
        const string callbackUrl = "https://api.test/webhooks/shopee";
        const string body = """{"code":3,"shop_id":99,"data":{"ordersn":"A1","status":"READY_TO_SHIP"}}""";

        // Act
        var signature = MarketplaceSignatureHelper.GenerateShopeeWebhookSign(partnerKey, callbackUrl, body);
        var valid = MarketplaceSignatureHelper.CompareHexSignatures(signature, signature);

        // Assert
        Assert.True(valid);
    }

    [Fact]
    public void CompareHexSignatures_ShouldReturnFalse_WhenSignatureIsWrong()
    {
        // Arrange
        const string computed = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        const string wrong = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

        // Act
        var result = MarketplaceSignatureHelper.CompareHexSignatures(computed, wrong);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyHmacSha256Hex_ShouldReturnTrue_WhenPayloadMatchesSecret()
    {
        // Arrange
        const string secret = "tiktok-secret";
        const string body = """{"type":"order_status_change"}""";
        var expected = MarketplaceSignatureHelper.GenerateTikTokShopWebhookSign(secret, body);

        // Act
        var valid = MarketplaceSignatureHelper.VerifyHmacSha256Hex(secret, body, expected);

        // Assert
        Assert.True(valid);
    }
}
