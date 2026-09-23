using System.Net;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TecFlow.Business.Integrations.Shopee;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Core.Enums;
using TecFlow.Database.Entity;
using TecFlow.Infrastructure.Services.Integrations.Shopee;
using TecFlow.Infrastructure.Services.LinkStrategies;
using TecFlow.Tests.Helpers;

namespace TecFlow.Tests.Unit.LinkStrategies;

public class ShopeeLinkConversionTests
{
    private static readonly Guid TestTenantId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private const string ExpectedUniversalSubId = "Loja Homolog";
    private static string ExpectedSubId => ShopeeCommissionUrlBuilder.BuildSubId(10, TestTenantId);

    [Fact]
    public async Task ShopeeIntegrationClient_WithCredentials_ShouldForwardHttpGet()
    {
        var requested = default(Uri);
        var handler = new StubHttpMessageHandler(request =>
        {
            requested = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"ok\":true}")
            };
        });

        var client = CreateIntegrationClient(MarketplaceTestOptionsFactory.ShopeeOptions(), handler);

        Assert.False(client.IsSandboxMode);

        using var response = await client.GetAsync("product/get_item_list");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(requested);
        Assert.Contains("product/get_item_list", requested!.AbsoluteUri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ShopeeIntegrationClient_WithoutCredentials_ShouldUseSandboxAndNotCallHttp()
    {
        var handler = new StubHttpMessageHandler(_ =>
            throw new InvalidOperationException("HTTP não deve ser chamado no sandbox."));

        var client = CreateIntegrationClient(EmptyShopeeOptions(), handler);

        Assert.True(client.IsSandboxMode);

        using var response = await client.GetAsync("product/get_item_list");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(ShopeeSandboxLinkBuilder.DefaultTrackingCode, body, StringComparison.Ordinal);
    }

    [Fact]
    public void ShopeeIntegrationClient_SandboxUrl_ShouldContainTrackingAndSubId()
    {
        var client = CreateIntegrationClient(
            EmptyShopeeOptions(),
            new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        var subId = ShopeeCommissionUrlBuilder.BuildSubId(10, TestTenantId);
        var url = client.BuildSandboxTrackedUrl(ProductUrl, subId, ProductUrl);

        Assert.True(ShopeeCommissionUrlBuilder.TryGetQueryValue(url, ShopeeCommissionUrlBuilder.TrackingCodeQuery, out var tracking));
        Assert.Equal(ShopeeSandboxLinkBuilder.DefaultTrackingCode, tracking);
        Assert.True(ShopeeCommissionUrlBuilder.TryGetQueryValue(url, ShopeeCommissionUrlBuilder.SubIdQuery, out var actualSubId));
        Assert.Equal(subId, actualSubId);
        Assert.True(ShopeeCommissionUrlBuilder.TryGetQueryValue(url, ShopeeCommissionUrlBuilder.UniversalLinkQuery, out var universal));
        Assert.Equal(ProductUrl, universal);
    }

    [Fact]
    public async Task ShopeeAffiliateLinkClient_WithCredentials_ShouldReturnApiCustomLink()
    {
        var handler = StubHttpMessageHandler.WithJsonResponse(
            """{"data":{"customLink":"https://shopee.com.br/afiliado-convertido"}}""");

        var client = CreateAffiliateClient(MarketplaceTestOptionsFactory.ShopeeOptions(), handler);
        var link = await client.GenerateCustomLinkAsync(CreateStore(), ProductUrl, AffiliateId, customNickname: null);

        Assert.Contains("afiliado-convertido", link, StringComparison.OrdinalIgnoreCase);
        Assert.True(ShopeeCommissionUrlBuilder.TryGetQueryValue(link, ShopeeCommissionUrlBuilder.SubIdQuery, out var subId));
        Assert.Equal(ExpectedSubId, subId);
    }

    [Fact]
    public async Task ShopeeAffiliateLinkClient_WithoutCredentials_ShouldReturnSandboxUrlWithoutHttp()
    {
        var handler = new StubHttpMessageHandler(_ =>
            throw new InvalidOperationException("HTTP não deve ser chamado no sandbox."));

        var client = CreateAffiliateClient(EmptyShopeeOptions(), handler);
        var link = await client.GenerateCustomLinkAsync(CreateStore(), ProductUrl, AffiliateId, "nick");

        Assert.Contains(ShopeeSandboxLinkBuilder.DefaultTrackingCode, link, StringComparison.Ordinal);
        Assert.Contains("tracking_code=", link, StringComparison.OrdinalIgnoreCase);
        Assert.True(ShopeeCommissionUrlBuilder.TryGetQueryValue(link, ShopeeCommissionUrlBuilder.SubIdQuery, out var subId));
        Assert.Equal(ExpectedSubId, subId);
        Assert.True(ShopeeCommissionUrlBuilder.TryGetQueryValue(link, ShopeeCommissionUrlBuilder.AffiliateIdQuery, out var affiliate));
        Assert.Equal(ShopeeCommissionUrlBuilder.HomologAffiliateId, affiliate);
        Assert.DoesNotContain("Contacte o administrador", link, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ShopeeLinkStrategy_WithoutCredentials_ShouldGenerateUniversalDeeplink()
    {
        var strategy = CreateShopeeStrategy(new PassthroughUrlExpansionService());

        var link = await strategy.GenerateDeepLinkAsync(ProductUrl, Guid.NewGuid(), AffiliateId);

        Assert.Contains("/universal-link/product/123/456", link, StringComparison.Ordinal);
        Assert.True(ShopeeCommissionUrlBuilder.TryGetQueryValue(link, ShopeeCommissionUrlBuilder.SubIdQuery, out var subId));
        Assert.Equal(ExpectedUniversalSubId, subId);
        Assert.DoesNotContain("tracking_code=", link, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("https://shopee.com.br/produto-i.123.456", true)]
    [InlineData("https://www.shopee.com.br/produto-i.123.456", true)]
    [InlineData("https://s.shopee.com.br/abc123", true)]
    [InlineData("https://br.shp.ee/taeej22s", true)]
    [InlineData("https://br.shp.ee/taeej22s?fromSource=copy_link&smtt=0.0.9", true)]
    [InlineData("https://shp.ee/abc", true)]
    [InlineData("https://shope.ee/xyz", true)]
    [InlineData("shopee://product?itemid=999&shopid=888", true)]
    [InlineData("https://example.com/produto", false)]
    public void PlatformLinkResolver_ShouldRecognizeShopeeDomains(string url, bool expected)
    {
        var resolver = CreateResolver();

        if (expected)
        {
            var strategy = resolver.Resolve(url);
            Assert.Equal("Shopee", strategy.PlatformName);
            Assert.True(strategy.CanProcess(url));
            return;
        }

        var ex = Assert.Throws<AffiliateLinkGenerationException>(() => resolver.Resolve(url));
        Assert.Contains("não suportada", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UrlExpansionService_ShouldFollow301And302LocationToCanonicalShopeeUrl()
    {
        var hops = 0;
        var handler = new StubHttpMessageHandler(request =>
        {
            var host = request.RequestUri!.Host;
            if (host.Contains("s.shopee.com.br", StringComparison.OrdinalIgnoreCase))
            {
                hops++;
                return new HttpResponseMessage(HttpStatusCode.MovedPermanently)
                {
                    Headers = { Location = new Uri("https://shopee.com.br/intermediate") }
                };
            }

            if (host.Equals("shopee.com.br", StringComparison.OrdinalIgnoreCase)
                && request.RequestUri.AbsolutePath.Equals("/intermediate", StringComparison.OrdinalIgnoreCase))
            {
                hops++;
                return new HttpResponseMessage(HttpStatusCode.Found)
                {
                    Headers = { Location = new Uri("https://shopee.com.br/Cadeira-Gamer-i.555.777") }
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var service = new UrlExpansionService(
            new StubHttpClientFactory(handler),
            NullLogger<UrlExpansionService>.Instance);

        var expanded = await service.ExpandUrlAsync("https://s.shopee.com.br/abc");

        Assert.Equal("https://shopee.com.br/Cadeira-Gamer-i.555.777", expanded);
        Assert.Equal(2, hops);
        Assert.True(ShopeeProductUrlParser.TryParse(expanded, out var ids));
        Assert.Equal("555", ids.ShopId);
        Assert.Equal("777", ids.ItemId);
    }

    [Fact]
    public async Task ShopeeLinkStrategy_ShouldUnshortenAndExtractShopAndItemIds()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri!.Host.Contains("s.shopee.com.br", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.Found)
                {
                    Headers = { Location = new Uri("https://shopee.com.br/product/888/999") }
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var strategy = CreateShopeeStrategy(
            new UrlExpansionService(new StubHttpClientFactory(handler), NullLogger<UrlExpansionService>.Instance));

        var link = await strategy.GenerateDeepLinkAsync("https://s.shopee.com.br/share", Guid.NewGuid(), AffiliateId);

        Assert.Contains("/universal-link/product/888/999", link, StringComparison.Ordinal);
        Assert.True(ShopeeCommissionUrlBuilder.TryGetQueryValue(link, ShopeeCommissionUrlBuilder.SubIdQuery, out var subId));
        Assert.Equal(ExpectedUniversalSubId, subId);
    }

    [Fact]
    public async Task ShopeeLinkStrategy_ShouldUnshortenBrShpEeAndExtractShopAndItemIds()
    {
        const string shortUrl = "https://br.shp.ee/taeej22s?fromSource=copy_link&smtt=0.0.9";
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri!.Host.Contains("br.shp.ee", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.Found)
                {
                    Headers = { Location = new Uri("https://shopee.com.br/product/123456/789012") }
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var strategy = CreateShopeeStrategy(
            new UrlExpansionService(new StubHttpClientFactory(handler), NullLogger<UrlExpansionService>.Instance));

        var link = await strategy.GenerateDeepLinkAsync(shortUrl, Guid.NewGuid(), AffiliateId);

        Assert.True(ShopeeLinkHostMatcher.IsShopeeUrl(shortUrl));
        Assert.True(ShopeeLinkHostMatcher.IsShortenerUrl(shortUrl));
        Assert.Contains("/universal-link/product/123456/789012", link, StringComparison.Ordinal);
        Assert.DoesNotContain("extraParams", link, StringComparison.OrdinalIgnoreCase);
        Assert.True(ShopeeCommissionUrlBuilder.TryGetQueryValue(link, ShopeeCommissionUrlBuilder.SubIdQuery, out var subId));
        Assert.Equal(ExpectedUniversalSubId, subId);
    }

    [Fact]
    public async Task ShopeeLinkStrategy_ShouldConvertBrShpEeWithoutProductIdsUsingHomologFallback()
    {
        const string shortUrl = "https://br.shp.ee/taeej22s";
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri!.Host.Contains("br.shp.ee", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var strategy = CreateShopeeStrategy(
            new UrlExpansionService(new StubHttpClientFactory(handler), NullLogger<UrlExpansionService>.Instance));

        var link = await strategy.GenerateDeepLinkAsync(shortUrl, Guid.NewGuid(), AffiliateId);

        Assert.Equal("taeej22s", ShopeeProductUrlParser.TryExtractShortHash(shortUrl));
        Assert.Contains("/universal-link/product/999999/888888", link, StringComparison.Ordinal);
        Assert.Contains("utm_source=affiliate", link, StringComparison.Ordinal);
        Assert.Contains("sub_id=tecflow_test", link, StringComparison.Ordinal);
        Assert.Contains("short_hash=taeej22s", link, StringComparison.Ordinal);
    }

    [Fact]
    public void MarketplaceUrlDetector_ShouldRecognizeBrShpEe()
    {
        var detected = TecFlow.SharedUi.Helpers.MarketplaceUrlDetector.Detect(
            "https://br.shp.ee/taeej22s?fromSource=copy_link&smtt=0.0.9");

        Assert.NotNull(detected);
        Assert.Equal(TecFlow.SharedUi.Helpers.SupportedMarketplaceKey.Shopee, detected!.Key);
    }

    [Fact]
    public async Task ShopeeAffiliateLinkClient_WhenApiReturnsEmptyInHomolog_ShouldBuildTrackedProductUrl()
    {
        var handler = StubHttpMessageHandler.WithJsonResponse("""{"data":{}}""");
        var client = CreateAffiliateClient(MarketplaceTestOptionsFactory.ShopeeOptions(), handler, "Homologacao");
        var link = await client.GenerateCustomLinkAsync(CreateStore(), ProductUrl, AffiliateId, customNickname: null);

        Assert.Contains("/product/123/456", link, StringComparison.Ordinal);
        Assert.True(ShopeeCommissionUrlBuilder.TryGetQueryValue(link, ShopeeCommissionUrlBuilder.AffiliateIdQuery, out var affiliate));
        Assert.Equal(ShopeeCommissionUrlBuilder.HomologAffiliateId, affiliate);
    }

    [Theory]
    [InlineData("https://shopee.com.br/Cadeira-Gamer-i.123456.789012", "123456", "789012")]
    [InlineData("https://shopee.com.br/product-name-i.123456.7891011", "123456", "7891011")]
    [InlineData("https://shopee.com.br/Cadeira-Gamer-Titans-Atlas-Preta-i.1226120317.22197624557", "1226120317", "22197624557")]
    [InlineData("https://shopee.com.br/Cadeira-Gamer-Titans-Atlas-Preta-i.1226120317.22197624557?extraParams=%7B%22foo%22%3A1%7D&sp_atk=abc&xptdk=xyz", "1226120317", "22197624557")]
    [InlineData("https://shopee.com.br/Cadeira-Gamer-Olympians-Poseidon-Preta-e-Vermelha-i.1226120317.19899301031", "1226120317", "19899301031")]
    [InlineData("https://shopee.com.br/product/111/222", "111", "222")]
    [InlineData("https://shopee.com.br/universal-link/product/999999/888888", "999999", "888888")]
    [InlineData("https://shopee.com.br/item/333/444", "333", "444")]
    [InlineData("https://shopee.com.br/produto?shopid=555&itemid=666", "555", "666")]
    [InlineData("https://shopee.com.br/product/foo/bar?shopId=123456&itemId=7891011", "123456", "7891011")]
    [InlineData("https://shopee.com.br/produto?item_id=777&shop_id=888", "888", "777")]
    [InlineData("https://shopee.com.br/shop/123456/item/7891011", "123456", "7891011")]
    [InlineData("https://shopee.com.br/Nome-Produto.123456.7891011", "123456", "7891011")]
    public void ShopeeProductUrlParser_ShouldExtractShopIdAndItemId(string url, string shopId, string itemId)
    {
        Assert.True(ShopeeProductUrlParser.TryParse(url, out var ids));
        Assert.Equal(shopId, ids.ShopId);
        Assert.Equal(itemId, ids.ItemId);
    }

    [Fact]
    public void ShopeeProductUrlParser_ShouldSanitizeNoisyQueryParamsFromTitansAtlasUrl()
    {
        const string dirty =
            "https://shopee.com.br/Cadeira-Gamer-Titans-Atlas-Preta-i.1226120317.22197624557"
            + "?extraParams=%7B%22display_model%22%3A%7B%22id%22%3A0%7D%7D&sp_atk=token&xptdk=track&utm_source=share";

        var clean = ShopeeProductUrlParser.Sanitize(dirty);

        Assert.Equal(
            "https://shopee.com.br/Cadeira-Gamer-Titans-Atlas-Preta-i.1226120317.22197624557",
            clean);
        Assert.DoesNotContain("extraParams", clean, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sp_atk", clean, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("xptdk", clean, StringComparison.OrdinalIgnoreCase);
        Assert.True(ShopeeProductUrlParser.TryParse(clean, out var ids));
        Assert.Equal("1226120317", ids.ShopId);
        Assert.Equal("22197624557", ids.ItemId);
    }

    [Fact]
    public void ShopeeProductUrlParser_ParseOrThrow_ShouldReturnFriendlyUnrecognizedMessage()
    {
        var ex = Assert.Throws<AffiliateLinkGenerationException>(
            () => ShopeeProductUrlParser.ParseOrThrow("https://shopee.com.br/categoria/moveis"));

        Assert.Equal(ShopeeProductUrlParser.UnrecognizedLinkMessage, ex.Message);
    }

    [Fact]
    public async Task ShopeeLinkStrategy_ShouldConvertTitansAtlasDesktopUrlWithDisplayModelExtraParams()
    {
        const string original =
            "https://shopee.com.br/Cadeira-Gamer-Titans-Atlas-Preta-i.1226120317.22197624557"
            + "?extraParams=%7B%22display_model_id%22%3A199163985057%2C%22model_selection_logic%22%3A3%7D";

        Assert.True(ShopeeLinkStrategy.TryExtractDesktopProductIds(original, out var extracted));
        Assert.Equal("1226120317", extracted.ShopId);
        Assert.Equal("22197624557", extracted.ItemId);
        Assert.DoesNotContain("199163985057", extracted.ShopId, StringComparison.Ordinal);

        Assert.True(ShopeeProductUrlParser.TryParseDesktopItem(original, out var parsed));
        Assert.Equal("1226120317", parsed.ShopId);
        Assert.Equal("22197624557", parsed.ItemId);

        var context = new AffiliateLinkGenerationContext { UserId = 10 };
        var strategy = CreateShopeeStrategy(new PassthroughUrlExpansionService(), context: context);
        var link = await strategy.GenerateDeepLinkAsync(original, Guid.NewGuid(), AffiliateId);

        Assert.False(string.IsNullOrWhiteSpace(link));
        Assert.Contains("/universal-link/product/1226120317/22197624557", link, StringComparison.Ordinal);
        Assert.DoesNotContain("extraParams", link, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("199163985057", link, StringComparison.Ordinal);
        Assert.True(ShopeeCommissionUrlBuilder.TryGetQueryValue(link, ShopeeCommissionUrlBuilder.SubIdQuery, out var subId));
        Assert.Equal(ExpectedUniversalSubId, subId);
        Assert.Equal(link, context.OfficialShortenedShopeeUrl);
    }

    [Fact]
    public async Task ShopeeLinkStrategy_ShouldConvertPoseidonChairWhenOfficialShortenerFails()
    {
        const string original =
            "https://shopee.com.br/Cadeira-Gamer-Olympians-Poseidon-Preta-e-Vermelha-i.1226120317.19899301031";

        Assert.True(ShopeeLinkStrategy.TryExtractDesktopProductIds(original, out var extracted));
        Assert.Equal("1226120317", extracted.ShopId);
        Assert.Equal("19899301031", extracted.ItemId);

        var context = new AffiliateLinkGenerationContext { UserId = 10 };
        var strategy = CreateShopeeStrategy(
            new PassthroughUrlExpansionService(),
            environmentName: "Production",
            context: context);

        var link = await strategy.GenerateDeepLinkAsync(original, Guid.NewGuid(), AffiliateId);

        Assert.False(string.IsNullOrWhiteSpace(link));
        Assert.Contains("/universal-link/product/1226120317/19899301031", link, StringComparison.Ordinal);
        Assert.Equal(link, context.OfficialShortenedShopeeUrl);
        Assert.True(ShopeeCommissionUrlBuilder.TryGetQueryValue(link, ShopeeCommissionUrlBuilder.SubIdQuery, out var subId));
        Assert.Equal(ExpectedUniversalSubId, subId);
    }

    [Fact]
    public void ShopeeOfficialShortUrl_ShouldKeepBrShpEeFromOriginalAndIgnoreExtraParams()
    {
        const string original =
            "https://br.shp.ee/taeej22s?extraParams=%7B%22display_model_id%22%3A1%7D";

        var resolved = ShopeeOfficialShortUrl.Resolve(
            "https://shopee.com.br/product/1/2",
            original,
            new ShopeeProductUrlIds("1", "2"));

        Assert.Equal("https://br.shp.ee/taeej22s", resolved);
    }

    [Fact]
    public async Task ShopeeLinkStrategy_ShouldBuildUniversalDeeplinkWithFriendlyNameSubId()
    {
        var strategy = CreateShopeeStrategy(new PassthroughUrlExpansionService());

        var link = await strategy.GenerateDeepLinkAsync(ProductUrl, Guid.NewGuid(), AffiliateId);

        Assert.StartsWith("https://shopee.com.br/universal-link/product/123/456?", link, StringComparison.Ordinal);
        Assert.True(ShopeeCommissionUrlBuilder.TryGetQueryValue(link, ShopeeCommissionUrlBuilder.SubIdQuery, out var subId));
        Assert.Equal(ExpectedUniversalSubId, subId);
        Assert.True(ShopeeCommissionUrlBuilder.ContainsEncodedQueryPair(link, ShopeeCommissionUrlBuilder.SubIdQuery, ExpectedUniversalSubId));
        Assert.False(ShopeeCommissionUrlBuilder.TryGetQueryValue(link, ShopeeCommissionUrlBuilder.DeepLinkQuery, out _));
    }

    [Fact]
    public async Task ShopeeLinkStrategy_ShouldUseTrackingIdAsSubIdWhenConfiguredOnStore()
    {
        var store = CreateStore();
        store.ShopId = "ul-1-loja-homolog";
        store.AffiliateTrackingId = "18325850271";
        var strategy = CreateShopeeStrategy(new PassthroughUrlExpansionService(), store: store);

        var link = await strategy.GenerateDeepLinkAsync(ProductUrl, Guid.NewGuid(), AffiliateId);

        Assert.Contains("/universal-link/product/123/456", link, StringComparison.Ordinal);
        Assert.True(ShopeeCommissionUrlBuilder.TryGetQueryValue(link, ShopeeCommissionUrlBuilder.SubIdQuery, out var subId));
        Assert.Equal("18325850271", subId);
    }

    [Fact]
    public async Task ShopeeLinkStrategy_ShouldMapNativeDeepLinkToUniversalDeeplink()
    {
        const string native = "shopee://product?itemid=999&shopid=888";
        var strategy = CreateShopeeStrategy(new PassthroughUrlExpansionService());

        var link = await strategy.GenerateDeepLinkAsync(native, Guid.NewGuid(), AffiliateId);

        Assert.True(ShopeeLinkHostMatcher.IsNativeDeepLink(native));
        Assert.Contains("/universal-link/product/888/999", link, StringComparison.Ordinal);
        Assert.True(ShopeeCommissionUrlBuilder.TryGetQueryValue(link, ShopeeCommissionUrlBuilder.SubIdQuery, out var subId));
        Assert.Equal(ExpectedUniversalSubId, subId);
    }

    [Fact]
    public async Task ShopeeLinkStrategy_ShouldSanitizeProductUrlAndEncodeCommissionQuery()
    {
        const string original =
            "https://shopee.com.br/produto-i.1.2?q=Cadeira%20Gamer%20%26%20Kids&extraParams=%7B%22x%22%3A1%7D&sp_atk=tok";
        var strategy = CreateShopeeStrategy(new PassthroughUrlExpansionService());

        var link = await strategy.GenerateDeepLinkAsync(original, Guid.NewGuid(), AffiliateId);

        Assert.DoesNotContain("extraParams", link, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sp_atk", link, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("q=Cadeira", link, StringComparison.Ordinal);
        Assert.Contains("/universal-link/product/1/2", link, StringComparison.Ordinal);
        Assert.True(ShopeeCommissionUrlBuilder.TryGetQueryValue(link, ShopeeCommissionUrlBuilder.SubIdQuery, out var subId));
        Assert.Equal(ExpectedUniversalSubId, subId);
        Assert.True(ShopeeCommissionUrlBuilder.ContainsEncodedQueryPair(link, ShopeeCommissionUrlBuilder.SubIdQuery, ExpectedUniversalSubId));
    }

    [Fact]
    public void ShopeeCommissionUrlBuilder_ShouldPutTrackingCodeAndSubIdOnQueryString()
    {
        var subId = ShopeeCommissionUrlBuilder.BuildSubId(10, TestTenantId);
        var url = ShopeeCommissionUrlBuilder.Merge(
            ProductUrl,
            ShopeeCommissionUrlBuilder.DefaultTrackingCode,
            subId,
            "https://shopee.com.br/product/123/456");

        Assert.Contains("tracking_code=", url, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sub_id=", url, StringComparison.OrdinalIgnoreCase);
        Assert.True(ShopeeCommissionUrlBuilder.ContainsEncodedQueryPair(
            url,
            ShopeeCommissionUrlBuilder.TrackingCodeQuery,
            ShopeeCommissionUrlBuilder.DefaultTrackingCode));
        Assert.True(ShopeeCommissionUrlBuilder.ContainsEncodedQueryPair(
            url,
            ShopeeCommissionUrlBuilder.SubIdQuery,
            subId));
        Assert.Equal("u10_t11111111222233334444555555555555", subId);
    }

    [Fact]
    public void ShopeeCommissionUrlBuilder_ShouldEncodeUniversalAndNativeDeepLinks()
    {
        const string universal = "https://shopee.com.br/product/888/999";
        const string native = "shopee://product?itemid=999&shopid=888";

        var url = ShopeeCommissionUrlBuilder.Merge(
            ProductUrl,
            trackingCode: "tecflow_sandbox_subid",
            subId: ExpectedSubId,
            universalLink: universal,
            deepLink: native);

        Assert.True(ShopeeCommissionUrlBuilder.TryGetQueryValue(url, ShopeeCommissionUrlBuilder.UniversalLinkQuery, out var decodedUniversal));
        Assert.Equal(universal, decodedUniversal);
        Assert.True(ShopeeCommissionUrlBuilder.TryGetQueryValue(url, ShopeeCommissionUrlBuilder.DeepLinkQuery, out var decodedDeep));
        Assert.Equal(native, decodedDeep);
        Assert.True(ShopeeCommissionUrlBuilder.ContainsEncodedQueryPair(url, ShopeeCommissionUrlBuilder.DeepLinkQuery, native));
        Assert.True(ShopeeCommissionUrlBuilder.ContainsEncodedQueryPair(url, ShopeeCommissionUrlBuilder.UniversalLinkQuery, universal));
        Assert.DoesNotContain("deep_link=shopee://product?itemid=999&shopid=888", url, StringComparison.Ordinal);
    }

    [Fact]
    public void ShopeeCommissionUrlBuilder_ShouldUrlEncodeSpecialCharactersInQuery()
    {
        const string tracking = "tecflow sandbox & kids";
        const string promoValue = "Cadeira Gamer & Kids";
        var source = "https://shopee.com.br/produto-i.1.2?q=" + ShopeeCommissionUrlBuilder.EncodeQueryComponent(promoValue);

        var url = ShopeeCommissionUrlBuilder.Merge(
            source,
            tracking,
            ExpectedSubId,
            "https://shopee.com.br/product/1/2");

        Assert.True(ShopeeCommissionUrlBuilder.ContainsEncodedQueryPair(url, ShopeeCommissionUrlBuilder.TrackingCodeQuery, tracking));
        Assert.True(ShopeeCommissionUrlBuilder.ContainsEncodedQueryPair(url, "q", promoValue));
        Assert.Contains("q=Cadeira%20Gamer%20%26%20Kids", url, StringComparison.Ordinal);
        Assert.Contains("tracking_code=tecflow%20sandbox%20%26%20kids", url, StringComparison.Ordinal);
        Assert.DoesNotContain("q=Cadeira Gamer & Kids", url, StringComparison.Ordinal);
    }

    [Fact]
    public void ShopeeCommissionUrlBuilder_ShouldPreserveFragmentAndRejectMissingUser()
    {
        var url = ShopeeCommissionUrlBuilder.Merge(
            "https://shopee.com.br/produto-i.1.2#oferta",
            ShopeeCommissionUrlBuilder.DefaultTrackingCode,
            ShopeeCommissionUrlBuilder.BuildSubId(42, Guid.Empty));

        Assert.EndsWith("#oferta", url, StringComparison.Ordinal);
        Assert.True(ShopeeCommissionUrlBuilder.TryGetQueryValue(url, ShopeeCommissionUrlBuilder.SubIdQuery, out var subId));
        Assert.Equal("u42", subId);

        Assert.Throws<AffiliateLinkGenerationException>(() => ShopeeCommissionUrlBuilder.BuildSubId(0, TestTenantId));
    }

    [Fact]
    public void ShopeeIntegrationClient_SandboxUrl_ShouldEncodeDeepLinkAndUserSubId()
    {
        var client = CreateIntegrationClient(
            EmptyShopeeOptions(),
            new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        const string native = "shopee://product?itemid=456&shopid=123";
        var url = client.BuildSandboxTrackedUrl(ProductUrl, ExpectedSubId, ProductUrl, native);

        Assert.True(ShopeeCommissionUrlBuilder.ContainsEncodedQueryPair(url, ShopeeCommissionUrlBuilder.SubIdQuery, ExpectedSubId));
        Assert.True(ShopeeCommissionUrlBuilder.ContainsEncodedQueryPair(url, ShopeeCommissionUrlBuilder.DeepLinkQuery, native));
        Assert.True(ShopeeCommissionUrlBuilder.ContainsEncodedQueryPair(url, ShopeeCommissionUrlBuilder.UniversalLinkQuery, ProductUrl));
    }

    private const string ProductUrl = "https://shopee.com.br/produto-i.123.456";
    private const string AffiliateId = "aff-homolog";

    private static ShopeeLinkStrategy CreateShopeeStrategy(
        IUrlExpansionService expansion,
        string environmentName = "Homologacao",
        AffiliateLinkGenerationContext? context = null,
        IntegracaoLoja? store = null) =>
        new(
            expansion,
            new FixedStoreResolver(store ?? CreateStore()),
            context ?? new AffiliateLinkGenerationContext { UserId = 10 },
            EmptyShopeeOptions(),
            CreateHostEnvironment(environmentName),
            NullLogger<ShopeeLinkStrategy>.Instance);

    private static PlatformLinkResolver CreateResolver() =>
        new(
            [
                new ShopeeLinkStrategy(
                    new PassthroughUrlExpansionService(),
                    new FixedStoreResolver(CreateStore()),
                    new AffiliateLinkGenerationContext { UserId = 10 },
                    EmptyShopeeOptions(),
                    CreateHostEnvironment(),
                    NullLogger<ShopeeLinkStrategy>.Instance)
            ],
            NullLogger<PlatformLinkResolver>.Instance);

    private static ShopeeIntegrationClient CreateIntegrationClient(
        IOptions<ShopeeIntegrationOptions> options,
        HttpMessageHandler handler)
    {
        var http = new HttpClient(handler, disposeHandler: false)
        {
            BaseAddress = new Uri("https://partner.shopeemobile.com/api/v2/")
        };

        return new ShopeeIntegrationClient(
            http,
            options,
            NullLogger<ShopeeIntegrationClient>.Instance);
    }

    private static ShopeeAffiliateLinkClient CreateAffiliateClient(
        IOptions<ShopeeIntegrationOptions> options,
        HttpMessageHandler handler,
        string environmentName = "Homologacao")
    {
        var http = new HttpClient(handler, disposeHandler: false)
        {
            BaseAddress = new Uri("https://open-api.affiliate.shopee.com.br/")
        };

        return new ShopeeAffiliateLinkClient(
            http,
            options,
            NullLogger<ShopeeAffiliateLinkClient>.Instance,
            CreateHostEnvironment(environmentName));
    }

    private static IHostEnvironment CreateHostEnvironment(string name = "Homologacao")
    {
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(item => item.EnvironmentName).Returns(name);
        return environment.Object;
    }

    private static IOptions<ShopeeIntegrationOptions> EmptyShopeeOptions() =>
        Options.Create(new ShopeeIntegrationOptions
        {
            PartnerId = "",
            PartnerKey = "",
            AppKey = "",
            AppSecret = "",
            AppSignature = "",
            SandboxTrackingCode = ShopeeSandboxLinkBuilder.DefaultTrackingCode
        });

    private static IntegracaoLoja CreateStore() =>
        new()
        {
            Id = 1,
            UserId = 10,
            TenantId = TestTenantId,
            ShopId = "ul-10-loja-homolog",
            FriendlyName = "Loja Homolog",
            AccessToken = "token",
            PlatformType = MarketplaceType.Shopee
        };

    private sealed class PassthroughUrlExpansionService : IUrlExpansionService
    {
        public Task<string> ExpandUrlAsync(string shortenedUrl, CancellationToken cancellationToken = default) =>
            Task.FromResult(shortenedUrl);
    }

    private sealed class FixedStoreResolver : IIntegracaoLojaScopeResolver
    {
        private readonly IntegracaoLoja _store;

        public FixedStoreResolver(IntegracaoLoja store) => _store = store;

        public Task<IntegracaoLoja> ResolveAsync(
            Guid storeScopeId,
            int userId,
            MarketplaceType expectedPlatform,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_store);
    }

    private sealed class RecordingShopeeClient : IShopeeAffiliateLinkClient
    {
        public string LastExpandedUrl { get; private set; } = string.Empty;

        public Task<string> GenerateCustomLinkAsync(
            IntegracaoLoja store,
            string expandedProductUrl,
            string affiliateId,
            string? customNickname,
            CancellationToken cancellationToken = default)
        {
            LastExpandedUrl = expandedProductUrl;
            return Task.FromResult(expandedProductUrl);
        }
    }

    private sealed class ThrowingShopeeClient : IShopeeAffiliateLinkClient
    {
        public Task<string> GenerateCustomLinkAsync(
            IntegracaoLoja store,
            string expandedProductUrl,
            string affiliateId,
            string? customNickname,
            CancellationToken cancellationToken = default) =>
            throw new HttpRequestException("Falha simulada na API de encurtamento oficial da Shopee.");
    }
}
