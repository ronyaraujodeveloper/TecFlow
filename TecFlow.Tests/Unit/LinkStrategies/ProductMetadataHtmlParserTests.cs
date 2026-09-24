using TecFlow.Business.Service.LinkStrategies;

namespace TecFlow.Tests.Unit.LinkStrategies;

public class ProductMetadataHtmlParserTests
{
    [Fact]
    public void Parse_ShouldReadOpenGraphTitleAndPrice()
    {
        const string html = """
            <html><head>
            <meta property="og:title" content="Cadeira Gamer Pro" />
            <meta property="og:price:amount" content="1299.90" />
            <meta property="og:image" content="https://cdn.example.com/cadeira.jpg" />
            </head></html>
            """;

        var parsed = ProductMetadataHtmlParser.Parse(html, "https://shopee.com.br/cadeira-gamer-i.1.2");

        Assert.Equal("Cadeira Gamer Pro", parsed.ProductName);
        Assert.Equal(1299.90m, parsed.ProductPrice);
        Assert.Equal("https://cdn.example.com/cadeira.jpg", parsed.ProductImageUrl);
    }

    [Fact]
    public void Parse_ShouldReadJsonLdProductNameAndPrice()
    {
        const string html = """
            <html><body>
            <script type="application/ld+json">
            {"@type":"Product","name":"Notebook i5","offers":{"@type":"Offer","price":"3499,00","priceCurrency":"BRL"}}
            </script>
            </body></html>
            """;

        var parsed = ProductMetadataHtmlParser.Parse(html, "https://www.kabum.com.br/produto/123-notebook-i5");

        Assert.Equal("Notebook i5", parsed.ProductName);
        Assert.Equal(3499.00m, parsed.ProductPrice);
    }

    [Fact]
    public void Parse_ShouldFallbackToSlug_WhenHtmlHasNoMetadata()
    {
        var parsed = ProductMetadataHtmlParser.Parse("<html></html>", "https://www.amazon.com.br/dp/B0TESTASIN/cadeira-ergonomica-preta");

        Assert.Equal("Cadeira Ergonomica Preta", parsed.ProductName);
        Assert.Null(parsed.ProductPrice);
    }

    [Fact]
    public void FromUrlFallback_ShouldKeepConversionSafe_WhenScrapingWouldFail()
    {
        var fallback = ProductMetadataHtmlParser.FromUrlFallback("https://br.shp.ee/abc123/super-oferta-mouse");

        Assert.Equal("Super Oferta Mouse", fallback.ProductName);
        Assert.Null(fallback.ProductPrice);
        Assert.Null(fallback.ProductImageUrl);
    }

    [Fact]
    public void FormatBrl_ShouldUseBrazilianCurrency()
    {
        Assert.Contains("R$", ProductMetadataHtmlParser.FormatBrl(199.9m));
        Assert.Equal("—", ProductMetadataHtmlParser.FormatBrl(null));
    }
}
