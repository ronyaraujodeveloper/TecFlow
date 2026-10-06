using TecFlow.Business.Service.Groups;

namespace TecFlow.Tests.Unit.Groups;

public class StructuredOfferParserServiceTests
{
    private readonly StructuredOfferParserService _parser = new();

    [Fact]
    public void Parse_ShouldExtractTitlePriceCouponAndPrimaryUrl()
    {
        const string text =
            """
            🔥 Cadeira Gamer RGB
            ✅ R$ 50,91
            CUPOM: BRINCADEIRAS
            https://t.me/ofertaspelando
            https://shopee.com.br/produto-i.1.2
            """;

        var parsed = _parser.Parse(text);

        Assert.Equal("Cadeira Gamer RGB", parsed.ProductTitle);
        Assert.Equal(50.91m, parsed.Price);
        Assert.Equal("BRINCADEIRAS", parsed.CouponCode);
        Assert.Equal("https://shopee.com.br/produto-i.1.2", parsed.PrimaryProductUrl);
        Assert.Equal("Shopee", parsed.Platform);
    }

    [Fact]
    public void Parse_ShouldReadValorPrefixAndCouponWithHyphen()
    {
        const string text =
            """
            👌 Fone Bluetooth
            💲 Valor: R$479
            Cupom SURPRESAMELIMAIS
            https://www.mercadolivre.com.br/p/MLB123
            """;

        var parsed = _parser.Parse(text);

        Assert.Equal("Fone Bluetooth", parsed.ProductTitle);
        Assert.Equal(479m, parsed.Price);
        Assert.Equal("SURPRESAMELIMAIS", parsed.CouponCode);
        Assert.Contains("mercadolivre", parsed.PrimaryProductUrl, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Mercado Livre", parsed.Platform);
    }

    [Fact]
    public void FilterPersistableUrls_ShouldDropTelegramShowcaseWhenProductExists()
    {
        var parsed = _parser.Parse(
            "Produto X\nhttps://t.me/canal\nhttps://www.amazon.com.br/dp/B0TESTE123");
        var urls = StructuredOfferParserService.FilterPersistableUrls(
            ["https://t.me/canal", "https://www.amazon.com.br/dp/B0TESTE123"],
            parsed);

        Assert.Single(urls);
        Assert.Contains("amazon.com.br/dp", urls[0], StringComparison.OrdinalIgnoreCase);
    }
}
