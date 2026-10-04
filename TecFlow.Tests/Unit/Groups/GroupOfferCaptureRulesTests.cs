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

    [Fact]
    public void DetectPlatform_ShouldMapShopeeUrl()
    {
        var platform = GroupOfferCaptureRules.DetectPlatform("https://shopee.com.br/produto-i.1.2");
        Assert.Equal(MarketplaceType.Shopee, platform);
    }

    [Theory]
    [InlineData(10, 10)]
    [InlineData(24, 24)]
    [InlineData(48, 48)]
    [InlineData(99, 24)]
    public void ResolveLookbackHours_ShouldClampUnknownValues(int input, int expected) =>
        Assert.Equal(expected, GroupOfferCaptureRules.ResolveLookbackHours(input));

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
}
