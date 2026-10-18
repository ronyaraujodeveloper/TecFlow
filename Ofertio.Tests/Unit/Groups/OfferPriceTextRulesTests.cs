using Ofertio.Business.Integrations.Shopee;
using Ofertio.Business.Service.Groups;
using Ofertio.Business.Service.LinkStrategies;

namespace Ofertio.Tests.Unit.Groups;

public class OfferPriceTextRulesTests
{
    [Theory]
    [InlineData("🔥 Notebook Gamer\n12x R$ 604,92 sem juros\n💰 R$ 6.678,28 à vista")]
    [InlineData("🔥 Notebook Gamer\n💰 R$ 6.678,28\nou 12x de R$ 604,92")]
    [InlineData("Notebook Gamer em até 12 x de R$ 604,92 ou R$ 6.678,28 no Pix")]
    [InlineData("Notebook Gamer R$ 604,92/mês ou R$ 6.678,28")]
    [InlineData("Notebook Gamer\nDe R$ 7.999,00 por R$ 6.678,28\n12x R$ 604,92")]
    public void TryExtractCashPrice_ShouldIgnoreInstallmentsAndOldPrice(string text)
    {
        Assert.Equal(6678.28m, OfferPriceTextRules.TryExtractCashPrice(text));
    }

    [Theory]
    [InlineData("Sabão Líquido Omo 3L\nR$ 16,60 / unidade\nR$ 55,00 à vista")]
    [InlineData("Sabão Líquido Omo 3L\n💰 R$ 55,00 (R$ 16,60 por frasco)")]
    [InlineData("Sabão Líquido Omo 3L\nunidade R$ 16,60\nTotal R$ 55,00")]
    [InlineData("Sabão Líquido Omo 3L R$ 16,60/un - leve o kit por R$ 55,00")]
    public void TryExtractCashPrice_ShouldIgnoreUnitPrice(string text)
    {
        Assert.Equal(55.00m, OfferPriceTextRules.TryExtractCashPrice(text));
    }

    [Fact]
    public void TryExtractCashPrice_ShouldKeepTotalForMultiUnitPack()
    {
        Assert.Equal(159.90m, OfferPriceTextRules.TryExtractCashPrice("Kit Cerveja R$ 159,90 por 3 unidades"));
    }

    [Fact]
    public void TryExtractCashPrice_ShouldReturnNull_WhenOnlyInstallmentIsInformed()
    {
        Assert.Null(OfferPriceTextRules.TryExtractCashPrice("Notebook Gamer 12x R$ 604,92 sem juros"));
    }

    [Theory]
    [InlineData("Fone Bluetooth R$ 89,90", 89.90)]
    [InlineData("Fone Bluetooth R$ 19.90", 19.90)]
    [InlineData("Smart TV R$ 1.999", 1999)]
    [InlineData("✅ 49,99", 49.99)]
    public void TryExtractCashPrice_ShouldKeepRegularFormats(string text, double expected)
    {
        Assert.Equal((decimal)expected, OfferPriceTextRules.TryExtractCashPrice(text));
    }

    [Fact]
    public void TelegramParsers_ShouldExtractCashPriceInsteadOfInstallment()
    {
        const string text = "🔥 Notebook Gamer Acer Nitro\n12x R$ 604,92\nR$ 6.678,28 à vista\nhttps://shopee.com.br/produto-i.1.2";

        Assert.Equal(6678.28m, GroupOfferCaptureRules.TryExtractTelegramPrice(text));
        Assert.Equal(6678.28m, new StructuredOfferParserService().Parse(text).Price);
        Assert.Equal(6678.28m, GroupOfferCaptureRules.ExtractPrice(text, "https://shopee.com.br/produto-i.1.2"));
    }

    [Fact]
    public void TryExtractTelegramPrice_ShouldSkipBareInstallmentMultiplier()
    {
        Assert.Equal(6678.28m, GroupOfferCaptureRules.TryExtractTelegramPrice("Notebook 12x 604,92 ou 6.678,28"));
    }

    [Theory]
    [InlineData(667.00, 6678.28, true)]
    [InlineData(6678.28, 667.00, true)]
    [InlineData(6678.28, 6499.00, false)]
    [InlineData(null, 6678.28, false)]
    [InlineData(667.00, 667828000000, false)]
    public void IsSignificantDivergence_ShouldFlagOnlyLargePlausibleGaps(double? saved, double page, bool expected)
    {
        Assert.Equal(expected, OfferPriceTextRules.IsSignificantDivergence((decimal?)saved, (decimal)page));
    }

    [Fact]
    public void HtmlParser_ShouldSkipInstallmentAndRawScaledShopeePrice()
    {
        const string html = """
            <html><body>
            {"price_min":667828000000}
            <div>12x <span>R$ 604,92</span></div>
            <div>R$ 6.678,28</div>
            </body></html>
            """;

        var parsed = ProductMetadataHtmlParser.Parse(html, "https://www.kabum.com.br/produto/123");

        Assert.Equal(6678.28m, parsed.ProductPrice);
    }

    [Theory]
    [InlineData("<div class=\"product-status\">Produto esgotado</div>")]
    [InlineData("<script>{\"item\":{\"is_sold_out\":true}}</script>")]
    [InlineData("<script>{\"item_status\":\"sold_out\"}</script>")]
    [InlineData("<button class=\"btn sold-out\">Comprar</button>")]
    public void ShopeeIsSoldOut_ShouldDetectSoldOutMarkers(string html)
    {
        Assert.True(ShopeeProductPageRules.IsSoldOut(html));
    }

    [Fact]
    public void ShopeeIsSoldOut_ShouldIgnoreAvailablePage()
    {
        Assert.False(ShopeeProductPageRules.IsSoldOut("<script>{\"is_sold_out\":false,\"stock\":12}</script><button>Comprar agora</button>"));
    }

    [Fact]
    public void ShopeeTryExtractActivePrice_ShouldReadMetaOrJsonLd()
    {
        Assert.Equal(
            6678.28m,
            ShopeeProductPageRules.TryExtractActivePrice("<meta property=\"product:price:amount\" content=\"6678.28\" />"));
        Assert.Equal(
            6678.28m,
            ShopeeProductPageRules.TryExtractActivePrice(
                "<script type=\"application/ld+json\">{\"@type\":\"Product\",\"offers\":{\"@type\":\"AggregateOffer\",\"lowPrice\":\"6678.28\"}}</script>"));
    }
}
