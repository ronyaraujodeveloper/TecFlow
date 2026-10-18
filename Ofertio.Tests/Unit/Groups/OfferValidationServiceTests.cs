using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Groups;
using Ofertio.Core.Entities;
using Ofertio.Core.Enums;
using Ofertio.Infrastructure.Services.Groups;

namespace Ofertio.Tests.Unit.Groups;

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
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task ValidateProductPageStatusAsync_ShouldFollowSocialRedirect_UntilProductPage()
    {
        var handler = new StubHandler
        {
            Html = "<html>Produto</html>",
            FinalUrlByRequest = request => request.Contains("/social/", StringComparison.Ordinal)
                ? "https://produto.mercadolivre.com.br/MLB-4012345678-fone-_JM"
                : request
        };
        var service = new OfferValidationService(
            new StubMetadata(),
            new StubResolver("https://www.mercadolivre.com.br/social/mastertechjr?matt_tool=88888888"),
            new HttpClient(handler),
            NullLogger<OfferValidationService>.Instance);

        var page = await service.ValidateProductPageStatusAsync("https://www.mercadolivre.com.br/social/mastertechjr?matt_tool=88888888");

        Assert.True(page.IsAvailable);
        Assert.Equal("https://produto.mercadolivre.com.br/MLB-4012345678-fone-_JM", page.FinalUrl);
    }

    [Fact]
    public async Task ValidateProductPageStatusAsync_ShouldStillRejectNonMercadoLivreStorefrontWithoutRequest()
    {
        var handler = new StubHandler { Html = "<html>Loja</html>" };
        var service = new OfferValidationService(
            new StubMetadata(),
            new StubResolver("https://www.mercadolivre.com.br/perfil/LOJA123"),
            new HttpClient(handler),
            NullLogger<OfferValidationService>.Instance);

        var page = await service.ValidateProductPageStatusAsync("https://www.mercadolivre.com.br/perfil/LOJA123");

        Assert.Equal(GroupOfferStatuses.VitrineGenerica, page.Status);
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task ValidateProductPageStatusAsync_ShouldValidateFocusedProduct_WhenShortLinkLandsOnAffiliateShowcase()
    {
        var handler = new StubHandler { Html = "<html>Produto</html>" };
        var service = new OfferValidationService(
            new StubMetadata(),
            new StubResolver("https://www.mercadolivre.com.br/social/achados?item_id=MLB4012345678&matt_tool=55555555&reco_client=home_affiliate-profile"),
            new HttpClient(handler),
            NullLogger<OfferValidationService>.Instance);

        var page = await service.ValidateProductPageStatusAsync("https://meli.la/abc123");

        Assert.True(page.IsAvailable);
        Assert.Equal(GroupOfferStatuses.Ativo, page.Status);
        Assert.Equal("https://www.mercadolivre.com.br/p/MLB4012345678", page.FinalUrl);
        Assert.Equal("https://www.mercadolivre.com.br/p/MLB4012345678", handler.LastRequestUri);
    }

    [Fact]
    public async Task ValidateProductPageStatusAsync_ShouldRefetchFocusedProduct_WhenRedirectEndsInAffiliateShowcase()
    {
        var handler = new StubHandler
        {
            Html = "<html>Produto</html>",
            FinalUrlByRequest = request => request.Contains("meli.la", StringComparison.Ordinal)
                ? "https://www.mercadolivre.com.br/social/achados?product_id=MLB19700000&matt_d2id=x"
                : request
        };
        var service = new OfferValidationService(
            new StubMetadata(),
            new StubResolver("https://meli.la/abc123"),
            new HttpClient(handler),
            NullLogger<OfferValidationService>.Instance);

        var page = await service.ValidateProductPageStatusAsync("https://meli.la/abc123");

        Assert.True(page.IsAvailable);
        Assert.Equal("https://www.mercadolivre.com.br/p/MLB19700000", page.FinalUrl);
        Assert.Equal(2, handler.Calls);
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
    public async Task ValidateProductPageStatusAsync_ShouldMarkMercadoLivreEsgotado_WhenPageShowsOnlyRelatedProducts()
    {
        var service = CreateService(
            "<html><body><h2>Produtos relacionados</h2><div class=\"carousel\">Fone A</div></body></html>",
            HttpStatusCode.OK,
            "https://www.mercadolivre.com.br/p/MLB19700000");

        var page = await service.ValidateProductPageStatusAsync("https://www.mercadolivre.com.br/p/MLB19700000");

        Assert.False(page.IsAvailable);
        Assert.Equal(GroupOfferStatuses.Esgotado, page.Status);
    }

    [Fact]
    public async Task ValidateProductPageStatusAsync_ShouldMarkMercadoLivreInativo_WhenListingIsPaused()
    {
        var service = CreateService(
            "<html><body><p>Anúncio pausado</p><h2>Produtos relacionados</h2></body></html>",
            HttpStatusCode.OK,
            "https://produto.mercadolivre.com.br/MLB-4012345678-fone-_JM");

        var page = await service.ValidateProductPageStatusAsync("https://produto.mercadolivre.com.br/MLB-4012345678-fone-_JM");

        Assert.False(page.IsAvailable);
        Assert.Equal(GroupOfferStatuses.Inativo, page.Status);
    }

    [Fact]
    public async Task ValidateProductPageStatusAsync_ShouldMarkMercadoLivreEsgotado_WhenRedirectedToSearch()
    {
        var handler = new StubHandler
        {
            Html = "<html><body>Resultados</body></html>",
            FinalUrlByRequest = _ => "https://lista.mercadolivre.com.br/fone-bluetooth"
        };
        var service = new OfferValidationService(
            new StubMetadata(),
            new StubResolver("https://produto.mercadolivre.com.br/MLB-4012345678-fone-_JM"),
            new HttpClient(handler),
            NullLogger<OfferValidationService>.Instance);

        var page = await service.ValidateProductPageStatusAsync("https://meli.la/abc123");

        Assert.False(page.IsAvailable);
        Assert.Equal(GroupOfferStatuses.Esgotado, page.Status);
    }

    [Fact]
    public async Task ValidateProductPageStatusAsync_ShouldKeepMercadoLivreActive_WhenBuyBoxIsPresent()
    {
        var service = CreateService(
            "<html><body><button>Comprar agora</button><span>Cor Azul: Sem estoque</span><h2>Produtos relacionados</h2></body></html>",
            HttpStatusCode.OK,
            "https://www.mercadolivre.com.br/p/MLB19700000");

        var page = await service.ValidateProductPageStatusAsync("https://www.mercadolivre.com.br/p/MLB19700000");

        Assert.True(page.IsAvailable);
        Assert.Equal(GroupOfferStatuses.Ativo, page.Status);
    }

    [Fact]
    public async Task ValidateAsync_ShouldUseAmazonBuyBoxTotal_InsteadOfUnitPrice()
    {
        const string html = """
            <html><head><meta property="og:title" content="Sabão Líquido Omo 3L - Kit com 3" /></head><body>
            <div id="corePriceDisplay_desktop_feature_div">
              <span class="a-price aok-align-center priceToPay"><span class="a-offscreen">R$ 55,00</span></span>
              <span class="a-size-mini pricePerUnit">(<span class="a-price a-text-price" data-a-size="mini"><span class="a-offscreen">R$ 16,60</span></span> / unidade)</span>
            </div></body></html>
            """;
        var service = CreateService(html, HttpStatusCode.OK, "https://www.amazon.com.br/dp/B0TESTE123");

        var result = await service.ValidateAsync("https://www.amazon.com.br/dp/B0TESTE123", 16.60m);

        Assert.True(result.IsAvailable);
        Assert.Equal(55.00m, result.Price);
        Assert.Equal(GroupOfferStatuses.PrecoAlterado, result.Status);
    }

    [Fact]
    public async Task ValidateAsync_ShouldMarkShopeeSoldOutAsEsgotado()
    {
        var service = CreateService(
            "<html><body><div>Produto esgotado</div></body></html>",
            HttpStatusCode.OK,
            "https://shopee.com.br/notebook-i.1.2");

        var result = await service.ValidateAsync("https://shopee.com.br/notebook-i.1.2", 6678.28m);

        Assert.False(result.IsAvailable);
        Assert.Equal(GroupOfferStatuses.Esgotado, result.Status);
    }

    [Fact]
    public async Task ValidateAsync_ShouldUseShopeeActivePriceAndFlagChange()
    {
        const string html = """
            <html><head>
            <meta property="og:title" content="Notebook Gamer Acer Nitro" />
            <meta property="product:price:amount" content="6678.28" />
            </head><body>{"price_min":667828000000}</body></html>
            """;
        var service = CreateService(html, HttpStatusCode.OK, "https://shopee.com.br/notebook-i.1.2");

        var result = await service.ValidateAsync("https://shopee.com.br/notebook-i.1.2", 667m);

        Assert.True(result.IsAvailable);
        Assert.Equal(6678.28m, result.Price);
        Assert.Equal(GroupOfferStatuses.PrecoAlterado, result.Status);
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

        public Func<string, string>? FinalUrlByRequest { get; set; }

        public int Calls { get; private set; }

        public string? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            var requested = request.RequestUri?.ToString() ?? string.Empty;
            LastRequestUri = requested;
            var final = FinalUrlByRequest?.Invoke(requested) ?? FinalUrl ?? requested;
            var response = new HttpResponseMessage(Status)
            {
                Content = new StringContent(Html),
                RequestMessage = new HttpRequestMessage(HttpMethod.Get, final)
            };
            return Task.FromResult(response);
        }
    }
}
