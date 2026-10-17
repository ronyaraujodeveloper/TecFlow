using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging.Abstractions;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Groups;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Infrastructure.Services.Groups;

namespace TecFlow.Tests.Unit.Groups;

public class OfferValidationServiceTests
{
    [Fact]
    public async Task ValidateProductPageStatusAsync_ShouldMarkUnavailable_WhenHtmlHasKnownError()
    {
        var html = "<html><body>Ops! Produto não encontrado</body></html>";
        var service = CreateService(html, HttpStatusCode.OK, "https://shopee.com.br/produto-i.1.2");

        var page = await service.ValidateProductPageStatusAsync("https://shp.ee/abc");

        Assert.False(page.IsAvailable);
        Assert.Equal(GroupOfferStatuses.Esgotado, page.Status);
    }

    [Fact]
    public async Task ValidateAsync_ShouldRejectGenericStorefront_WhenResolvedUrlIsSocialProfile()
    {
        var handler = new StubHandler { Html = "<html>Produto</html>" };
        var service = new OfferValidationService(
            new StubMetadata(),
            new StubResolver("https://www.mercadolivre.com.br/social/loja-vendedor?matt_tool=1"),
            new HttpClient(handler),
            NullLogger<OfferValidationService>.Instance);

        var result = await service.ValidateAsync("https://meli.la/abc123", 99m);

        Assert.False(result.IsAvailable);
        Assert.Equal(GroupOfferStatuses.VitrineGenerica, result.Status);
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task ValidateProductPageStatusAsync_ShouldRejectGenericStorefront_WhenRedirectEndsInStorefront()
    {
        var service = CreateService(
            "<html>Loja</html>",
            HttpStatusCode.OK,
            "https://www.mercadolivre.com.br/perfil-vendedor/LOJA123");

        var page = await service.ValidateProductPageStatusAsync("https://produto.mercadolivre.com.br/MLB-1-x-_JM");

        Assert.False(page.IsAvailable);
        Assert.Equal(GroupOfferStatuses.VitrineGenerica, page.Status);
    }

    [Fact]
    public async Task ValidateAsync_ShouldMarkInactive_WhenListingIsClosed()
    {
        var service = CreateService(
            "<html><body><h2>Publicação finalizada</h2></body></html>",
            HttpStatusCode.OK,
            "https://produto.mercadolivre.com.br/MLB-1-x-_JM");

        var result = await service.ValidateAsync("https://produto.mercadolivre.com.br/MLB-1-x-_JM", 50m);

        Assert.False(result.IsAvailable);
        Assert.Equal(GroupOfferStatuses.Inativo, result.Status);
    }

    [Fact]
    public async Task ValidateAsync_ShouldFallbackToOpenGraphTitleAndPrice()
    {
        const string html = """
            <html><head>
            <meta property="og:title" content="Headset Gamer RGB" />
            <meta property="product:price:amount" content="189.90" />
            <meta property="og:image" content="https://cdn.example.com/headset.jpg" />
            </head></html>
            """;
        var service = CreateService(html, HttpStatusCode.OK, "https://www.kabum.com.br/produto/headset-gamer-rgb");

        var result = await service.ValidateAsync("https://www.kabum.com.br/produto/headset-gamer-rgb", null);

        Assert.True(result.IsAvailable);
        Assert.Equal("Headset Gamer RGB", result.ProductName);
        Assert.Equal(189.90m, result.Price);
        Assert.Equal("https://cdn.example.com/headset.jpg", result.ImageUrl);
    }

    private static OfferValidationService CreateService(string html, HttpStatusCode status, string finalUrl)
    {
        var handler = new StubHandler
        {
            Html = html,
            Status = status,
            FinalUrl = finalUrl
        };
        var http = new HttpClient(handler);
        return new OfferValidationService(
            new StubMetadata(),
            new StubResolver(finalUrl),
            http,
            NullLogger<OfferValidationService>.Instance);
    }

    private sealed class StubResolver : IUrlResolverService
    {
        private readonly string _url;

        public StubResolver(string url) => _url = url;

        public Task<UrlResolverResultDto> ResolveCanonicalAsync(string capturedUrl, CancellationToken cancellationToken = default) =>
            Task.FromResult(new UrlResolverResultDto
            {
                CanonicalUrl = _url,
                IsMarketplace = true,
                Platform = MarketplaceType.Kabum
            });
    }

    private sealed class StubMetadata : IProductMetadataService
    {
        public Task<ProductMetadataDto> ExtractAsync(string productUrl, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ProductMetadataDto());
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        public string Html { get; set; } = string.Empty;

        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;

        public string? FinalUrl { get; set; }

        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            var response = new HttpResponseMessage(Status)
            {
                Content = new StringContent(Html),
                RequestMessage = new HttpRequestMessage(HttpMethod.Get, FinalUrl ?? request.RequestUri?.ToString())
            };
            return Task.FromResult(response);
        }
    }
}
