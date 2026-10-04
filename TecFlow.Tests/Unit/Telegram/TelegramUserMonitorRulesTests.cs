using TecFlow.Business.Service.Telegram;
using TecFlow.Core.Enums;

namespace TecFlow.Tests.Unit.Telegram;

public class TelegramUserMonitorRulesTests
{
    [Theory]
    [InlineData("https://shopee.com.br/produto-i.1.2", MarketplaceType.Shopee)]
    [InlineData("https://www.mercadolivre.com.br/produto", MarketplaceType.MercadoLivre)]
    [InlineData("https://www.amazon.com.br/dp/B0TESTE", MarketplaceType.Amazon)]
    [InlineData("https://www.magazineluiza.com.br/p/abc/123", MarketplaceType.MagazineLuiza)]
    [InlineData("https://pt.aliexpress.com/item/100.html", MarketplaceType.AliExpress)]
    [InlineData("https://www.casasbahia.com.br/produto/123", MarketplaceType.CasasBahia)]
    public void IsTrackedCommerceUrl_ShouldAcceptMarketplaces(string url, MarketplaceType expected)
    {
        Assert.True(TelegramUserMonitorRules.IsTrackedCommerceUrl(url, out var platform));
        Assert.Equal(expected, platform);
    }

    [Theory]
    [InlineData("https://bit.ly/abc123")]
    [InlineData("https://t.me/ofertas/1")]
    [InlineData("https://s.shopee.com.br/abc")]
    [InlineData("https://www.pelando.com.br/d/abc")]
    [InlineData("https://www.promobit.com.br/oferta/abc")]
    [InlineData("https://www.casasbahia.com.br/produto/123")]
    [InlineData("https://www.pontofrio.com.br/produto/123")]
    [InlineData("https://amzn.to/xyz")]
    public void IsTrackedCommerceUrl_ShouldAcceptShortenersAndDealPortals(string url) =>
        Assert.True(TelegramUserMonitorRules.IsTrackedCommerceUrl(url, out _));

    [Fact]
    public void ExtractHttpUrls_ShouldCapturePathAndQuery()
    {
        var urls = TelegramUserMonitorRules.ExtractHttpUrls(
            "veja https://www.amazon.com.br/dp/B0TEST?tag=x&ref=y fim");
        Assert.Contains("https://www.amazon.com.br/dp/B0TEST?tag=x&ref=y", urls);
    }

    [Fact]
    public void IsTrackedCommerceUrl_ShouldRejectUnrelatedLinks()
    {
        Assert.False(TelegramUserMonitorRules.IsTrackedCommerceUrl("https://example.com/oferta", out _));
    }

    [Fact]
    public void BuildChannelChatId_ShouldPrefixMinus100() =>
        Assert.Equal("-100123456789", TelegramUserMonitorRules.BuildChannelChatId(123456789));

    [Theory]
    [InlineData("+5511981656947abcHASH", "+5511981656947")]
    [InlineData("55 11 98165-6947", "+5511981656947")]
    public void SanitizePhoneInput_ShouldKeepE164Digits(string raw, string expected) =>
        Assert.Equal(expected, TelegramUserMonitorRules.SanitizePhoneInput(raw));

    [Fact]
    public void TryNormalizeE164Phone_ShouldRejectShortNumbers() =>
        Assert.False(TelegramUserMonitorRules.TryNormalizeE164Phone("+55119", out _));

    [Theory]
    [InlineData("12a34b5", "12345")]
    [InlineData("123456789", "12345")]
    public void SanitizePinInput_ShouldKeepFiveDigits(string raw, string expected) =>
        Assert.Equal(expected, TelegramUserMonitorRules.SanitizePinInput(raw));

    [Fact]
    public void TryParseApiId_ShouldAcceptNumericId()
    {
        Assert.True(TelegramUserMonitorRules.TryParseApiId("28471934", out var apiId));
        Assert.Equal(28471934, apiId);
        Assert.False(TelegramUserMonitorRules.TryParseApiId("TecFlow Monitor", out _));
    }

    [Fact]
    public void TryValidateApiIdField_ShouldRejectTokenAndChatId()
    {
        Assert.False(TelegramUserMonitorRules.TryValidateApiIdField("8743142006:AAHsecret", out _, out var tokenError));
        Assert.Equal(TelegramUserMonitorRules.ApiIdLooksLikeTokenMessage, tokenError);
        Assert.False(TelegramUserMonitorRules.TryValidateApiIdField("-1001234567890", out _, out var chatError));
        Assert.Equal(TelegramUserMonitorRules.ApiIdLooksLikeChatIdMessage, chatError);
        Assert.True(TelegramUserMonitorRules.TryValidateApiIdField("28471934", out var apiId, out _));
        Assert.Equal(28471934, apiId);
    }

    [Fact]
    public void TryNormalizeVerificationPin_ShouldRequireFiveDigits()
    {
        Assert.True(TelegramUserMonitorRules.TryNormalizeVerificationPin("12345", out var pin));
        Assert.Equal("12345", pin);
        Assert.False(TelegramUserMonitorRules.TryNormalizeVerificationPin("1234", out _));
    }

    [Fact]
    public void IsFormatError_ShouldDetectTokenAndE164Hints()
    {
        Assert.True(TelegramUserMonitorRules.IsFormatError(TelegramUserMonitorRules.ApiIdLooksLikeTokenMessage));
        Assert.True(TelegramUserMonitorRules.IsFormatError("Informe o telefone no formato internacional, por exemplo +5511981656947."));
        Assert.False(TelegramUserMonitorRules.IsFormatError("Não foi possível solicitar o código."));
    }
}
