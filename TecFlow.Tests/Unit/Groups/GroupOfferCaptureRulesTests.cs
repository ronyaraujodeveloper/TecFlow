using TecFlow.Business.Service.Groups;
using TecFlow.Business.Service.Telegram;
using TecFlow.Core.Enums;

namespace TecFlow.Tests.Unit.Groups;

public class GroupOfferCaptureRulesTests
{
    [Fact]
    public void ExtractPrice_ShouldReadBrazilianCurrencyFromText()
    {
        var price = GroupOfferCaptureRules.ExtractPrice(
            "Oferta relâmpago R$ 99,90 só hoje",
            "https://www.kabum.com.br/produto/cadeira-gamer");

        Assert.Equal(99.90m, price);
    }

    [Theory]
    [InlineData("Por R$ 1.299,00 hoje", 1299.00)]
    [InlineData("$ 49,90", 49.90)]
    [InlineData("R$1299", 1299)]
    public void ExtractPrice_ShouldAcceptStructuredTelegramRegex(string text, decimal expected)
    {
        var price = GroupOfferCaptureRules.ExtractPrice(text, "https://shopee.com.br/produto-i.1.2");
        Assert.Equal(expected, price);
    }

    [Fact]
    public void ContainsUnavailableProductPhrase_ShouldDetectShopeeStoreError()
    {
        const string html = "<html><body>Essa loja falhou ao carregar. Tente novamente.</body></html>";
        Assert.True(GroupOfferCaptureRules.ContainsUnavailableProductPhrase(html));
    }

    [Fact]
    public void NeedsStructuredFallback_ShouldDetectTruncatedTitle()
    {
        Assert.True(GroupOfferCaptureRules.NeedsStructuredFallback("Oferta", 10m));
        Assert.True(GroupOfferCaptureRules.NeedsStructuredFallback("Cadeira Gamer Pro", null));
        Assert.False(GroupOfferCaptureRules.NeedsStructuredFallback("Cadeira Gamer Pro", 99.90m));
    }

    [Fact]
    public void DetectPlatform_ShouldMapShopeeUrl()
    {
        var platform = GroupOfferCaptureRules.DetectPlatform("https://shopee.com.br/produto-i.1.2");
        Assert.Equal(MarketplaceType.Shopee, platform);
    }

    [Theory]
    [InlineData("https://shopee.com.br/produto-i.1.2", true)]
    [InlineData("https://www.mercadolivre.com.br/p/MLB1", true)]
    [InlineData("https://t.me/ofertas/1", false)]
    [InlineData("https://example.com/aviso", false)]
    public void HasDirectProductUrl_ShouldKeepCheckoutLinksOnly(string url, bool expected) =>
        Assert.Equal(expected, GroupOfferCaptureRules.HasDirectProductUrl(url));

    [Theory]
    [InlineData(10, 10)]
    [InlineData(24, 24)]
    [InlineData(48, 48)]
    [InlineData(99, 24)]
    public void ResolveLookbackHours_ShouldClampUnknownValues(int input, int expected) =>
        Assert.Equal(expected, GroupOfferCaptureRules.ResolveLookbackHours(input));

    [Theory]
    [InlineData(0, 25)]
    [InlineData(10, 10)]
    [InlineData(25, 25)]
    [InlineData(50, 50)]
    [InlineData(100, 100)]
    [InlineData(101, 25)]
    [InlineData(-8, 25)]
    public void ResolveOffersTake_ShouldAllowOnlyTenTwentyFiveFiftyOrHundred(int input, int expected) =>
        Assert.Equal(expected, GroupOfferCaptureRules.ResolveOffersTake(input));

    [Fact]
    public void ResolveOffersSkipFromPage_ShouldUseZeroBasedOffset() =>
        Assert.Equal(50, GroupOfferCaptureRules.ResolveOffersSkipFromPage(3, 25));

    [Fact]
    public void ResolveOffersSkip_ShouldRejectNegative() =>
        Assert.Equal(0, GroupOfferCaptureRules.ResolveOffersSkip(-10));

    [Fact]
    public void Telegram_ShouldCaptureGroupButIgnorePrivateConversionOnly()
    {
        const string json = """
            {"update_id":1,"message":{"message_id":9,"chat":{"id":-100,"type":"supergroup","title":"Ofertas"},"text":"https://shopee.com.br/item","from":{"is_bot":false}}}
            """;
        using var document = System.Text.Json.JsonDocument.Parse(json);
        var incoming = TelegramBotRules.TryParseIncoming(document.RootElement);

        Assert.NotNull(incoming);
        Assert.True(incoming!.IsGroup);
        Assert.True(TelegramBotRules.ShouldCaptureGroup(incoming));
        Assert.True(TelegramBotRules.ShouldIgnore(incoming));
    }

    [Fact]
    public void TryParseTelegramChatId_ShouldStripChannelPrefix()
    {
        Assert.Equal("-100123", GroupOfferCaptureRules.TryParseTelegramChatId("Telegram:-100123"));
        Assert.Equal("-100123", GroupOfferCaptureRules.TryParseTelegramChatId("-100123"));
        Assert.Null(GroupOfferCaptureRules.TryParseTelegramChatId("Telegram:"));
    }
}
