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
    public void IsTrackedCommerceUrl_ShouldAcceptMarketplaces(string url, MarketplaceType expected)
    {
        Assert.True(TelegramUserMonitorRules.IsTrackedCommerceUrl(url, out var platform));
        Assert.Equal(expected, platform);
    }

    [Fact]
    public void IsTrackedCommerceUrl_ShouldRejectUnrelatedLinks()
    {
        Assert.False(TelegramUserMonitorRules.IsTrackedCommerceUrl("https://example.com/oferta", out _));
    }

    [Fact]
    public void BuildChannelChatId_ShouldPrefixMinus100() =>
        Assert.Equal("-100123456789", TelegramUserMonitorRules.BuildChannelChatId(123456789));
}
