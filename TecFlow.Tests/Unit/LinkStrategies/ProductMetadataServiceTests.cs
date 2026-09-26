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

    [Fact]
    public async Task ExtractAsync_ShouldUseShopeeSlugBeforeItemId_AndRejectNumericId()
    {
        const string url =
            "https://shopee.com.br/Lovito-Casual-Suti%C3%A3-B%C3%A1sico-E-Respir%C3%A1vel-Para-Todas-As-Esta%C3%A7%C3%B5es-Para-Mulheres-LNE37064-i.308244953.25901538592";
        var expansion = new StubExpansionService(url);
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "<html><head><title>25901538592</title></head><body>Opaanlp Nsbo</body></html>",
                System.Text.Encoding.UTF8,
                "text/html")
        });
        var service = new ProductMetadataService(
            expansion,
            new StubHttpClientFactory(handler),
            NullLogger<ProductMetadataService>.Instance);

        var result = await service.ExtractAsync(url);

        Assert.Equal(
            "Lovito Casual Sutiã Básico E Respirável Para Todas As Estações Para Mulheres LNE37064",
            result.ProductName);
        Assert.DoesNotContain("25901538592", result.ProductName, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExtractAsync_ShouldIgnoreOpaanlpAndFallbackWithoutSavingAntiBotTitle()
    {
        var expansion = new StubExpansionService("https://s.shopee.com.br/8plUTWtg3e");
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "<html><head><title>Opaanlp</title></head><body>Nsbo verification captcha</body></html>",
                System.Text.Encoding.UTF8,
                "text/html")
        });
        var service = new ProductMetadataService(
            expansion,
            new StubHttpClientFactory(handler),
            NullLogger<ProductMetadataService>.Instance);

        var result = await service.ExtractAsync("https://s.shopee.com.br/8plUTWtg3e");

        Assert.Null(result.ProductName);
        Assert.NotEqual("Opaanlp", result.ProductName, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("Opaanlp", result.ProductName ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Nsbo", result.ProductName ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.True(ProductMetadataService.IsInvalidProductName("Opaanlp"));
        Assert.True(ProductMetadataService.IsInvalidProductName("Nsbo"));
    }

    [Fact]
    public async Task ExtractAsync_ShouldUnwrapTargetParameterAndUseShopeeSlug()
    {
        const string productUrl =
            "https://shopee.com.br/Lovito-Casual-Suti%C3%A3-B%C3%A1sico-E-Respir%C3%A1vel-Para-Todas-As-Esta%C3%A7%C3%B5es-Para-Mulheres-LNE37064-i.308244953.25901538592";
        var redirect =
            "https://shopee.com.br/authenticate?target=" + Uri.EscapeDataString(productUrl);
        var expansion = new StubExpansionService(redirect);
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "<html><head><title>Opaanlp Nsbo</title></head><body>Just a moment</body></html>",
                System.Text.Encoding.UTF8,
                "text/html")
        });
        var service = new ProductMetadataService(
            expansion,
            new StubHttpClientFactory(handler),
            NullLogger<ProductMetadataService>.Instance);

        var result = await service.ExtractAsync("https://s.shopee.com.br/8plUTWtg3e");

        Assert.Equal(
            "Lovito Casual Sutiã Básico E Respirável Para Todas As Estações Para Mulheres LNE37064",
            result.ProductName);
        Assert.DoesNotContain("Opaanlp", result.ProductName, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("25901538592", result.ProductName, StringComparison.Ordinal);
    }

    [Fact]
    public void TryParseShopeeItemIds_ShouldReadShopAndItemFromPathAndQuery()
    {
        Assert.True(ProductMetadataService.TryParseShopeeItemIds(
            "https://shopee.com.br/Lovito-Casual-Suti%C3%A3-B%C3%A1sico-E-Respir%C3%A1vel-Para-Todas-As-Esta%C3%A7%C3%B5es-Para-Mulheres-LNE37064-i.308244953.4062152607",
            out var shopFromPath,
            out var itemFromPath));
        Assert.Equal("308244953", shopFromPath);
        Assert.Equal("4062152607", itemFromPath);

        Assert.True(ProductMetadataService.TryParseShopeeItemIds(
            "https://shopee.com.br/product?sp_atk=abc&shopid=308244953&itemid=4062152607",
            out var shopFromQuery,
            out var itemFromQuery));
        Assert.Equal("308244953", shopFromQuery);
        Assert.Equal("4062152607", itemFromQuery);
        Assert.True(ProductMetadataService.TryParseShopeeItemIds(
            "https://shopee.com.br/product/1890496775/23499652945",
            out var shopFromProduct,
            out var itemFromProduct));
        Assert.Equal("1890496775", shopFromProduct);
        Assert.Equal("23499652945", itemFromProduct);
        Assert.Equal(28.70m, ProductMetadataService.ConvertShopeePrice(2_870_000_000m));
    }

    [Fact]
    public async Task ExtractMetadataAsync_ShouldRouteShopeeProductPathToItemApi()
    {
        const string json = """
            {"data":{"itemid":23499652945,"shopid":1890496775,"name":"Lovito Casual Sutiã Básico E Respirável Para Todas As Estações Para Mulheres LNE37064","price":2870000000,"image":"lovito-img"}}
            """;
        var requested = new List<string>();
        var expansion = new StubExpansionService("https://shopee.com.br/product/1890496775/23499652945");
        var handler = new StubHttpMessageHandler(request =>
        {
            requested.Add(request.RequestUri?.ToString() ?? string.Empty);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        });
        var service = new ProductMetadataService(
            expansion,
            new StubHttpClientFactory(handler),
            NullLogger<ProductMetadataService>.Instance);

        var result = await service.ExtractMetadataAsync("https://s.shopee.com.br/8plUTWtg3e");

        Assert.Equal("Shopee", service.LastResolvedPlatform);
        Assert.Contains(requested, url => url.Contains("/api/v4/item/get", StringComparison.Ordinal)
            && url.Contains("shopid=1890496775", StringComparison.Ordinal)
            && url.Contains("itemid=23499652945", StringComparison.Ordinal));
        Assert.Equal(
            "Lovito Casual Sutiã Básico E Respirável Para Todas As Estações Para Mulheres LNE37064",
            result.ProductName);
        Assert.Equal(28.70m, result.ProductPrice);
        Assert.True(ProductMetadataService.IsInvalidProductName("Opaanlp"));
        Assert.True(ProductMetadataService.IsInvalidProductName("Nsbo"));
        Assert.True(ProductMetadataService.IsInvalidProductName("Shopee Brasil"));
        Assert.True(ProductMetadataService.IsInvalidProductName("Produto"));
    }

    [Fact]
    public async Task ExtractMetadataAsync_ShouldUseMagaluHandler_WithoutShopeeApi()
    {
        var requested = new List<string>();
        var expansion = new StubExpansionService(
            "https://www.magazineluiza.com.br/smartphone-samsung-galaxy-a15/p/218434100/te/smsg/");
        var handler = new StubHttpMessageHandler(request =>
        {
            requested.Add(request.RequestUri?.ToString() ?? string.Empty);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """<html><head><meta property="og:title" content="Smartphone Samsung Galaxy A15" /><meta property="og:price:amount" content="899.90" /></head></html>""",
                    System.Text.Encoding.UTF8,
                    "text/html")
            };
        });
        var service = new ProductMetadataService(
            expansion,
            new StubHttpClientFactory(handler),
            NullLogger<ProductMetadataService>.Instance);

        var result = await service.ExtractMetadataAsync("https://magalu.me/abc");

        Assert.Equal("MagazineLuiza", service.LastResolvedPlatform);
        Assert.DoesNotContain(requested, url => url.Contains("/api/v4/item/get", StringComparison.Ordinal));
        Assert.Equal("Smartphone Samsung Galaxy A15", result.ProductName);
        Assert.Equal(899.90m, result.ProductPrice);
    }

    [Fact]
    public async Task ExtractMetadataAsync_ShouldUseMercadoLivreHandler_WithoutShopeeApi()
    {
        var requested = new List<string>();
        var expansion = new StubExpansionService(
            "https://www.mercadolivre.com.br/fone-bluetooth-tws-com-cancelamento/p/MLB123456789");
        var handler = new StubHttpMessageHandler(request =>
        {
            requested.Add(request.RequestUri?.ToString() ?? string.Empty);
            return new HttpResponseMessage(HttpStatusCode.Forbidden);
        });
        var service = new ProductMetadataService(
            expansion,
            new StubHttpClientFactory(handler),
            NullLogger<ProductMetadataService>.Instance);

        var result = await service.ExtractMetadataAsync("https://mercadolivre.com/sec/abc");

        Assert.Equal("MercadoLivre", service.LastResolvedPlatform);
        Assert.DoesNotContain(requested, url => url.Contains("/api/v4/item/get", StringComparison.Ordinal));
        Assert.Equal("Fone Bluetooth Tws Com Cancelamento", result.ProductName);
        Assert.Null(result.ProductPrice);
    }

    [Fact]
    public async Task ExtractMetadataAsync_ShouldUseTikTokHandler_WithoutShopeeApi()
    {
        var requested = new List<string>();
        var expansion = new StubExpansionService("https://shop.tiktok.com/view/product/1729382258339663534");
        var handler = new StubHttpMessageHandler(request =>
        {
            requested.Add(request.RequestUri?.ToString() ?? string.Empty);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """<html><head><meta property="og:title" content="Base Líquida Matte 30ml" /><meta property="og:price:amount" content="49.90" /></head></html>""",
                    System.Text.Encoding.UTF8,
                    "text/html")
            };
        });
        var service = new ProductMetadataService(
            expansion,
            new StubHttpClientFactory(handler),
            NullLogger<ProductMetadataService>.Instance);

        var result = await service.ExtractMetadataAsync("https://vt.tiktok.com/ZSabc/");

        Assert.Equal("TikTokShop", service.LastResolvedPlatform);
        Assert.DoesNotContain(requested, url => url.Contains("/api/v4/item/get", StringComparison.Ordinal));
        Assert.Equal("Base Líquida Matte 30ml", result.ProductName);
        Assert.Equal(49.90m, result.ProductPrice);
    }

    [Fact]
    public async Task ExtractAsync_ShouldParseShopeeItemApiNameAndMicroPrice()
    {
        const string url =
            "https://shopee.com.br/Lovito-Casual-Suti%C3%A3-B%C3%A1sico-E-Respir%C3%A1vel-Para-Todas-As-Esta%C3%A7%C3%B5es-Para-Mulheres-LNE37064-i.308244953.4062152607";
        const string json = """
            {"data":{"itemid":4062152607,"shopid":308244953,"name":"Lovito Casual Sutiã Básico E Respirável Para Todas As Estações Para Mulheres LNE37064","price":2870000000,"price_min":2870000000,"image":"br-11134207-7r98o-lovito"}}
            """;
        var expansion = new StubExpansionService(url);
        string? requestedApi = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            requestedApi = request.RequestUri?.ToString();
            if (requestedApi?.Contains("/api/v4/item/get", StringComparison.Ordinal) == true)
            {
                Assert.Contains("itemid=4062152607", requestedApi, StringComparison.Ordinal);
                Assert.Contains("shopid=308244953", requestedApi, StringComparison.Ordinal);
                var userAgent = request.Headers.TryGetValues("User-Agent", out var values)
                    ? string.Join(" ", values)
                    : request.Headers.UserAgent.ToString();
                Assert.Contains("Chrome/122.0.0.0", userAgent, StringComparison.Ordinal);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        });
        var service = new ProductMetadataService(
            expansion,
            new StubHttpClientFactory(handler),
            NullLogger<ProductMetadataService>.Instance);

        var result = await service.ExtractAsync("https://s.shopee.com.br/8plUTWtg3e");

        Assert.Contains("/api/v4/item/get", requestedApi, StringComparison.Ordinal);
        Assert.Equal(
            "Lovito Casual Sutiã Básico E Respirável Para Todas As Estações Para Mulheres LNE37064",
            result.ProductName);
        Assert.Equal(28.70m, result.ProductPrice);
        Assert.Equal("R$ 28,70", ProductMetadataHtmlParser.FormatBrl(result.ProductPrice));
        Assert.Equal(
            "https://down-br.img.susercontent.com/file/br-11134207-7r98o-lovito",
            result.ProductImageUrl);
        Assert.DoesNotContain("Opaanlp", result.ProductName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExtractMetadataAsync_ShouldWarmUpShopeeHomepageBeforeItemApi()
    {
        const string json = """
            {"data":{"itemid":4062152607,"shopid":308244953,"name":"Lovito Casual Sutiã Básico E Respirável Para Todas As Estações Para Mulheres LNE37064","price":2870000000}}
            """;
        var requested = new List<string>();
        var expansion = new StubExpansionService(
            "https://shopee.com.br/Lovito-Casual-Suti%C3%A3-B%C3%A1sico-E-Respir%C3%A1vel-Para-Todas-As-Esta%C3%A7%C3%B5es-Para-Mulheres-LNE37064-i.308244953.4062152607");
        var handler = new StubHttpMessageHandler(request =>
        {
            requested.Add(request.RequestUri?.ToString() ?? string.Empty);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        });
        var service = new ProductMetadataService(
            expansion,
            new StubHttpClientFactory(handler),
            NullLogger<ProductMetadataService>.Instance);

        var result = await service.ExtractMetadataAsync("https://s.shopee.com.br/8plUTWtg3e");

        Assert.Contains(requested, url => url.TrimEnd('/').Equals("https://shopee.com.br", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(requested, url => url.Contains("/api/v4/item/get", StringComparison.Ordinal));
        Assert.True(requested.FindIndex(url => url.TrimEnd('/').Equals("https://shopee.com.br", StringComparison.OrdinalIgnoreCase))
            < requested.FindIndex(url => url.Contains("/api/v4/item/get", StringComparison.Ordinal)));
        Assert.Equal(
            "Lovito Casual Sutiã Básico E Respirável Para Todas As Estações Para Mulheres LNE37064",
            result.ProductName);
        Assert.Equal(28.70m, result.ProductPrice);
        Assert.DoesNotContain("Opaanlp", result.ProductName, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Nsbo", result.ProductName, StringComparison.OrdinalIgnoreCase);

        using var cookieHandler = ProductMetadataService.CreateShopeeCookieHandler();
        Assert.True(cookieHandler.UseCookies);
        Assert.NotNull(cookieHandler.CookieContainer);
        Assert.True(cookieHandler.AllowAutoRedirect);
        Assert.Equal(
            DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
            cookieHandler.AutomaticDecompression);
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
