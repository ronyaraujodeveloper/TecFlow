using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Infrastructure.Services.LinkStrategies;
using TecFlow.Tests.Helpers;

namespace TecFlow.Tests.Unit.LinkStrategies;

public class ProductMetadataServiceTests
{
    [Fact]
    public async Task ExtractAsync_ShouldParseOpenGraphAfterUnshorten()
    {
        var expansion = new StubExpansionService("https://www.kabum.com.br/produto/cadeira-gamer-pro");
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """<html><head><meta property="og:title" content="Cadeira Gamer Pro" /><meta property="og:price:amount" content="199.90" /></head></html>""")
        });
        var service = new ProductMetadataService(
            expansion,
            new StubHttpClientFactory(handler),
            NullLogger<ProductMetadataService>.Instance);

        var result = await service.ExtractAsync("https://br.shp.ee/xyz");

        Assert.Equal("https://br.shp.ee/xyz", expansion.LastUrl);
        Assert.Equal("Cadeira Gamer Pro", result.ProductName);
        Assert.Equal(199.90m, result.ProductPrice);
    }

    [Fact]
    public async Task ExtractAsync_ShouldFallbackToSlug_WhenMarketplaceBlocksScraping()
    {
        var expansion = new StubExpansionService("https://www.mercadolivre.com.br/sec/mouse-gamer-rgb");
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden));
        var service = new ProductMetadataService(
            expansion,
            new StubHttpClientFactory(handler),
            NullLogger<ProductMetadataService>.Instance);

        var result = await service.ExtractAsync("https://mercadolivre.com/sec/abc");

        Assert.Equal("Mouse Gamer Rgb", result.ProductName);
        Assert.Null(result.ProductPrice);
    }

    [Fact]
    public async Task ExtractAsync_ShouldExtractShopeeLovitoNameAndPrice()
    {
        const string expanded =
            "https://shopee.com.br/Lovito-Casual-Suti%C3%A3-Sem-Aro-i.18325850271.2559123456";
        const string html = """
            <html><head>
            <meta property="og:title" content="Lovito Casual Sutiã Sem Aro | Shopee Brasil" />
            <meta property="product:price:amount" content="28.70" />
            </head>
            <body>
            <span itemprop="price">28.70</span>
            <script type="application/ld+json">{"@type":"Product","name":"Lovito Casual Sutiã Sem Aro","offers":{"price":28.70}}</script>
            </body></html>
            """;

        var expansion = new StubExpansionService(expanded);
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(html, System.Text.Encoding.UTF8, "text/html")
        });
        var service = new ProductMetadataService(
            expansion,
            new StubHttpClientFactory(handler),
            NullLogger<ProductMetadataService>.Instance);

        var result = await service.ExtractAsync("https://s.shopee.com.br/lovito");

        Assert.Equal("Lovito Casual Sutiã Sem Aro", result.ProductName);
        Assert.DoesNotContain("| Shopee", result.ProductName, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(28.70m, result.ProductPrice);
        Assert.Equal("R$ 28,70", ProductMetadataHtmlParser.FormatBrl(result.ProductPrice));
    }

    [Fact]
    public async Task ExtractAsync_ShouldUseExpandedShopeeSlugAsPrimaryProductName()
    {
        const string url =
            "https://shopee.com.br/Lovito-Casual-Suti%C3%A3-B%C3%A1sico-E-Respir%C3%A1vel-Para-Todas-As-Esta%C3%A7%C3%B5es-Para-Mulheres-LNE37064-i.308244953.4062152607";
        var expansion = new StubExpansionService(url);
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "<html><head><title>Opaanlp Nsbo | Shopee Brasil Captcha</title></head><body>{\"price_min\":28.70,\"price\":28.70}</body></html>",
                System.Text.Encoding.UTF8,
                "text/html")
        });
        var service = new ProductMetadataService(
            expansion,
            new StubHttpClientFactory(handler),
            NullLogger<ProductMetadataService>.Instance);

        var result = await service.ExtractAsync("https://s.shopee.com.br/8plUTWtg3e");

        Assert.Equal("https://s.shopee.com.br/8plUTWtg3e", expansion.LastUrl);
        Assert.Equal(
            "Lovito Casual Sutiã Básico E Respirável Para Todas As Estações Para Mulheres LNE37064",
            result.ProductName);
        Assert.Equal(28.70m, result.ProductPrice);
        Assert.Equal("R$ 28,70", ProductMetadataHtmlParser.FormatBrl(result.ProductPrice));
    }

    [Fact]
    public async Task ExtractAsync_ShouldDecodeShopeeSlug_WhenHtmlOmitsOpenGraph()
    {
        const string expanded =
            "https://shopee.com.br/Lovito-Casual-Suti%C3%A3-Sem-Aro-i.18325850271.2559123456";
        var expansion = new StubExpansionService(expanded);
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("<html><body>blocked</body></html>")
        });
        var service = new ProductMetadataService(
            expansion,
            new StubHttpClientFactory(handler),
            NullLogger<ProductMetadataService>.Instance);

        var result = await service.ExtractAsync("https://s.shopee.com.br/lovito");

        Assert.Equal("Lovito Casual Sutiã Sem Aro", result.ProductName);
        Assert.Null(result.ProductPrice);
    }

    private sealed class StubExpansionService : IUrlExpansionService
    {
        private readonly string _expanded;

        public StubExpansionService(string expanded) => _expanded = expanded;

        public string? LastUrl { get; private set; }

        public Task<string> ExpandUrlAsync(string shortenedUrl, CancellationToken cancellationToken = default)
        {
            LastUrl = shortenedUrl;
            return Task.FromResult(_expanded);
        }
    }
}
