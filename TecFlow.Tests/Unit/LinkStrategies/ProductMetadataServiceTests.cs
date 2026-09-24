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
        var expansion = new StubExpansionService("https://shopee.com.br/cadeira-gamer-pro");
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
