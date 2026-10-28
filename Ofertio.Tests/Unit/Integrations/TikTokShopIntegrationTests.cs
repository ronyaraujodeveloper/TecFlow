using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Ofertio.Business.Dto;
using Ofertio.Business.Integrations.Auth;
using Ofertio.Business.Integrations.Common;
using Ofertio.Business.Integrations.MercadoLivre;
using Ofertio.Business.Integrations.TikTokShop;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Master;
using Ofertio.Business.Service.Radar;
using Ofertio.Core.Entities;
using Ofertio.Core.Enums;
using Ofertio.Database;
using Ofertio.Database.Filter;
using Ofertio.Infrastructure.Services.Integrations.TikTokShop;
using Ofertio.Infrastructure.Services.Master;
using Ofertio.Infrastructure.Services.Radar;
using Ofertio.Tests.Helpers;
using Ofertio.Util.Security;

namespace Ofertio.Tests.Unit.Integrations;

public class TikTokShopIntegrationTests
{
    private const string SearchPath = "affiliate_creator/202405/open_collaborations/products/search";

    [Fact]
    public void ComputeTikTokShopSign_ShouldFollow202309Spec()
    {
        var query = new Dictionary<string, string?>
        {
            ["timestamp"] = "1700000000",
            ["app_key"] = "abc",
            ["sign"] = "ignorado",
            ["access_token"] = "ignorado",
            ["page_size"] = "20"
        };
        const string body = """{"title_keywords":["fone"]}""";
        var input = "secret" + "/" + SearchPath + "app_keyabc" + "page_size20" + "timestamp1700000000" + body + "secret";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("secret"));
        var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();

        Assert.Equal(expected, MarketplaceSignatureHelper.ComputeTikTokShopSign("secret", SearchPath, query, body));
        Assert.Equal(
            expected,
            MarketplaceSignatureHelper.GenerateTikTokShopSign(
                "abc",
                "secret",
                "/" + SearchPath,
                1700000000,
                body,
                new Dictionary<string, string?> { ["page_size"] = "20" }));
    }

    [Fact]
    public void SearchRules_ShouldBuildQueryAndBodyFromRequest()
    {
        var request = new TikTokShopProductSearchRequest
        {
            Query = "  fone bluetooth ",
            PageSize = 50,
            PageToken = "next-1",
            MinPrice = 10m,
            MaxPrice = 200m,
            MinCommissionRate = 1000
        };

        var query = TikTokShopAffiliateSearchRules.BuildQuery(request);
        var body = TikTokShopAffiliateSearchRules.BuildBody(request);

        Assert.Equal("20", query["page_size"]);
        Assert.Equal("next-1", query["page_token"]);
        Assert.Contains("\"title_keywords\":[\"fone bluetooth\"]", body);
        Assert.Contains("\"amount_ge\":10", body);
        Assert.Contains("\"amount_lt\":200", body);
        Assert.Contains("\"rate_ge\":1000", body);
    }

    [Fact]
    public void SearchRules_ShouldMapProductsWithCommissionInHundredthsOfPercent()
    {
        const string json = """
            {"code":0,"message":"Success","data":{"products":[
              {"id":"1729","title":"Fone Bluetooth","main_image_url":"https://img/1.jpg",
               "sales_price":{"minimum_amount":"79.90","maximum_amount":"99.90","currency":"BRL"},
               "original_price":{"minimum_amount":"129.90","currency":"BRL"},
               "commission":{"rate":1250,"amount":"9.99","currency":"BRL"},
               "units_sold":340,"shop":{"name":"Loja X"},
               "category_chains":[{"id":"100","local_name":"Eletrônicos","parent_id":"0"},{"id":"200","local_name":"Fones","parent_id":"100"}]},
              {"title":"sem id"}
            ],"next_page_token":"p2"}}
            """;

        var result = TikTokShopAffiliateSearchRules.Parse(json, out var errorCode, out _);

        Assert.Null(errorCode);
        var item = Assert.Single(result.Items);
        Assert.Equal(MarketplaceType.TikTokShop, item.PlatformType);
        Assert.Equal("1729", item.ProductId);
        Assert.Equal(79.90m, item.Price);
        Assert.Equal(129.90m, item.OriginalPrice);
        Assert.Equal(12.5m, item.CommissionPercent);
        Assert.Equal(9.99m, item.EstimatedCommission);
        Assert.Equal(AffiliateCommissionRules.SourceTikTokShopApi, item.CommissionSource);
        Assert.Equal(340, item.SoldQuantity);
        Assert.Equal("Loja X", item.SellerName);
        Assert.Equal("100", item.RootCategoryId);
        Assert.Equal("Fones", item.CategoryName);
        Assert.Equal("https://shop.tiktok.com/view/product/1729", item.SourceUrl);
    }

    [Fact]
    public void SearchRules_ShouldExposeEnvelopeErrorAndDetectRefusedToken()
    {
        var result = TikTokShopAffiliateSearchRules.Parse("""{"code":105002,"message":"Expired credentials"}""", out var errorCode, out var message);

        Assert.Equal(105002, errorCode);
        Assert.Equal("Expired credentials", message);
        Assert.Equal("Expired credentials", result.ErrorMessage);
        Assert.True(TikTokShopAffiliateSearchRules.IsTokenRefused(HttpStatusCode.OK, errorCode));
        Assert.True(TikTokShopAffiliateSearchRules.IsTokenRefused(HttpStatusCode.Unauthorized, null));
        Assert.False(TikTokShopAffiliateSearchRules.IsTokenRefused(HttpStatusCode.BadRequest, 12019001));
    }

    [Fact]
    public void CreatorLinkRules_ShouldBuildPathAndExtractLink()
    {
        Assert.Equal(
            "/affiliate_creator/202405/products/1729/promotion_link/generate",
            TikTokShopCreatorLinkRules.BuildPromotionLinkPath("affiliate_creator/202405/products/{product_id}/promotion_link/generate", "1729"));
        Assert.Equal(
            "https://vt.tiktok.com/abc",
            TikTokShopCreatorLinkRules.TryExtractPromotionLink("""{"code":0,"data":{"product_promotion_link":"https://vt.tiktok.com/abc"}}""", out _, out _));
        Assert.Null(TikTokShopCreatorLinkRules.TryExtractPromotionLink("""{"code":105001,"message":"invalid"}""", out var code, out _));
        Assert.Equal(105001, code);
        Assert.False(TikTokShopCreatorLinkRules.HasCreatorToken(HomologMarketplaceAuth.StubAccessToken));
        Assert.False(TikTokShopCreatorLinkRules.HasCreatorToken(null));
        Assert.True(TikTokShopCreatorLinkRules.HasCreatorToken("tts-real"));
    }

    [Fact]
    public void SearchCacheRules_ShouldNormalizeQueryInKeyAndClampMinutes()
    {
        Assert.Equal(
            TikTokShopSearchCacheRules.BuildKey(new TikTokShopProductSearchRequest { Query = "Fone  Bluetooth" }),
            TikTokShopSearchCacheRules.BuildKey(new TikTokShopProductSearchRequest { Query = " fone bluetooth" }));
        Assert.Equal(TikTokShopSearchCacheRules.DefaultMinutes, TikTokShopSearchCacheRules.ResolveMinutes("abc"));
        Assert.Equal(TikTokShopSearchCacheRules.MaxMinutes, TikTokShopSearchCacheRules.ResolveMinutes("99999"));
        Assert.Equal(0, TikTokShopSearchCacheRules.ResolveMinutes("0"));
    }

    [Fact]
    public async Task AffiliateApiService_ShouldRefreshRefusedMasterTokenRetryAndCache()
    {
        var dbName = Guid.NewGuid().ToString();
        await using (var seed = CreateContext(dbName))
        {
            seed.GlobalIntegrationTokens.Add(new GlobalIntegrationToken
            {
                Provider = MarketplaceType.TikTokShop,
                AccessToken = "old-token",
                RefreshToken = "refresh-1",
                ExpiresAt = DateTime.UtcNow.AddHours(5)
            });
            await seed.SaveChangesAsync();
        }

        var tokensSeen = new List<string>();
        var handler = new StubHttpMessageHandler(request =>
        {
            var token = request.Headers.GetValues("x-tts-access-token").Single();
            tokensSeen.Add(token);
            var json = token == "old-token"
                ? """{"code":105002,"message":"Expired credentials"}"""
                : """{"code":0,"data":{"products":[{"id":"1","title":"Fone","sales_price":{"minimum_amount":"50"}}]}}""";
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
        });
        var auth = new Mock<IMarketplaceAuthService>();
        auth.Setup(x => x.RefreshTikTokShopTokenAsync("refresh-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MarketplaceOAuthExchangeResult
            {
                Success = true,
                AccessToken = "new-token",
                RefreshToken = "refresh-2",
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = CreateApiService(handler, dbName, auth.Object, cache);
        var request = new TikTokShopProductSearchRequest { Query = "fone" };

        var first = await sut.SearchProductsAsync(request);
        var second = await sut.SearchProductsAsync(request);

        Assert.Single(first.Items);
        Assert.Single(second.Items);
        Assert.Equal(["old-token", "new-token"], tokensSeen);
        await using var check = CreateContext(dbName);
        var stored = await check.GlobalIntegrationTokens.SingleAsync();
        Assert.Equal("new-token", stored.AccessToken);
        Assert.Equal("refresh-2", stored.RefreshToken);
        Assert.Null(stored.LastError);
        Assert.NotNull(stored.LastRefreshAt);
    }

    [Fact]
    public async Task AffiliateApiService_ShouldReportUnconfiguredWithoutMasterToken()
    {
        var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("não deve chamar a API"));
        var sut = CreateApiService(handler, Guid.NewGuid().ToString(), Moq.Mock.Of<IMarketplaceAuthService>());

        var result = await sut.SearchProductsAsync(new TikTokShopProductSearchRequest { Query = "fone" });

        Assert.True(result.MissingCredentials);
    }

    [Fact]
    public async Task CatalogSearch_ShouldIncludeTikTokShopChannelAndOffers()
    {
        var tikTok = new Mock<ITikTokShopAffiliateApiService>();
        tikTok.Setup(x => x.SearchProductsAsync(It.Is<TikTokShopProductSearchRequest>(r => r.Query == "fone"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OfficialCatalogChannelResult.From(
            [
                new OfficialCatalogProductDto
                {
                    Platform = "TikTokShop",
                    PlatformType = MarketplaceType.TikTokShop,
                    ProductName = "Fone",
                    Price = 50m,
                    CommissionPercent = 10m,
                    CommissionSource = AffiliateCommissionRules.SourceTikTokShopApi,
                    SourceUrl = "https://shop.tiktok.com/view/product/1"
                }
            ]));
        var sut = CreateCatalogSearch(tikTok.Object);

        var result = await sut.SearchAsync(1, new OfficialCatalogSearchFilter
        {
            Keyword = "fone",
            MercadoLivre = false,
            Shopee = false,
            Amazon = false,
            TikTokShop = true
        });

        var offer = result.DataList!.Single(item => item.PlatformType == MarketplaceType.TikTokShop);
        Assert.Equal(5m, offer.EstimatedCommission);
        var channel = result.Channels.Single(c => c.Key == "TikTokShop");
        Assert.Equal("ok", channel.State);
        Assert.Equal("skipped", result.Channels.Single(c => c.Key == "MercadoLivre").State);
    }

    [Fact]
    public async Task CatalogSearch_ShouldShowMissingMasterAuthorizationOnTikTokBadge()
    {
        var tikTok = new Mock<ITikTokShopAffiliateApiService>();
        tikTok.Setup(x => x.SearchProductsAsync(It.IsAny<TikTokShopProductSearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OfficialCatalogChannelResult.Unconfigured());
        var sut = CreateCatalogSearch(tikTok.Object);

        var result = await sut.SearchAsync(1, new OfficialCatalogSearchFilter { Keyword = "fone" });

        var channel = result.Channels.Single(c => c.Key == "TikTokShop");
        Assert.Equal("missing", channel.State);
        Assert.Equal(OfficialCatalogSearchRules.TikTokShopMissingAccountMessage, channel.Message);
    }

    [Fact]
    public async Task CompleteTikTokShopAuthorizationAsync_ShouldRejectUserWithoutGlobalKeysPermission()
    {
        await using var context = CreateContext(Guid.NewGuid().ToString());
        context.UserAccounts.Add(User(MasterRoles.FinanceiroSaaS));
        await context.SaveChangesAsync();
        var auth = new Mock<IMarketplaceAuthService>();
        var sut = CreateGlobalService(context, auth.Object);

        var result = await sut.CompleteTikTokShopAuthorizationAsync(1, "auth-code");

        Assert.False(result.Success);
        auth.Verify(x => x.ExchangeTikTokShopCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Empty(context.GlobalIntegrationTokens);
    }

    [Fact]
    public async Task CompleteTikTokShopAuthorizationAsync_ShouldRejectSellerAccount()
    {
        await using var context = CreateContext(Guid.NewGuid().ToString());
        context.UserAccounts.Add(User(MasterRoles.SuperAdmin));
        await context.SaveChangesAsync();
        var auth = new Mock<IMarketplaceAuthService>();
        auth.Setup(x => x.ExchangeTikTokShopCodeAsync("auth-code", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Exchange(TikTokShopUserTypes.Seller));
        var sut = CreateGlobalService(context, auth.Object);

        var result = await sut.CompleteTikTokShopAuthorizationAsync(1, "auth-code");

        Assert.False(result.Success);
        Assert.Empty(context.GlobalIntegrationTokens);
    }

    [Fact]
    public async Task CompleteTikTokShopAuthorizationAsync_ShouldStoreCreatorMasterToken()
    {
        await using var context = CreateContext(Guid.NewGuid().ToString());
        context.UserAccounts.Add(User(MasterRoles.SuperAdmin));
        await context.SaveChangesAsync();
        var auth = new Mock<IMarketplaceAuthService>();
        auth.Setup(x => x.ExchangeTikTokShopCodeAsync("auth-code", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Exchange(TikTokShopUserTypes.Creator));
        var sut = CreateGlobalService(context, auth.Object);

        var result = await sut.CompleteTikTokShopAuthorizationAsync(1, "auth-code");

        Assert.True(result.Success);
        var stored = await context.GlobalIntegrationTokens.SingleAsync();
        Assert.Equal(MarketplaceType.TikTokShop, stored.Provider);
        Assert.Equal("tts-access", stored.AccessToken);
        Assert.Equal("open-1", stored.ExternalAccountId);
        Assert.Equal("Criador X", stored.ExternalAccountName);
        var status = await sut.GetTikTokShopStatusAsync();
        Assert.Equal(GlobalIntegrationRules.StatusActive, status.Status);
        Assert.Equal("Criador X", status.AccountName);
    }

    private static MarketplaceOAuthExchangeResult Exchange(int userType) => new()
    {
        Success = true,
        AccessToken = "tts-access",
        RefreshToken = "tts-refresh",
        ExpiresAt = DateTime.UtcNow.AddDays(7),
        RefreshExpiresAt = DateTime.UtcNow.AddDays(30),
        OpenId = "open-1",
        AccountName = "Criador X",
        UserType = userType
    };

    private static TikTokShopAffiliateApiService CreateApiService(
        HttpMessageHandler handler,
        string dbName,
        IMarketplaceAuthService auth,
        IMemoryCache? cache = null)
    {
        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory.Setup(x => x.CreateClient(TikTokShopAffiliateApiService.HttpClientName))
            .Returns(() => new HttpClient(handler, disposeHandler: false));
        var dbFactory = new Mock<IDbContextFactory<AppDbContext>>();
        dbFactory.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CreateContext(dbName));
        var options = new Mock<IOptionsMonitor<TikTokShopIntegrationOptions>>();
        options.Setup(x => x.CurrentValue).Returns(new TikTokShopIntegrationOptions
        {
            AppKey = "app-key",
            AppSecret = "app-secret",
            SearchCacheMinutes = "15"
        });
        return new TikTokShopAffiliateApiService(
            httpFactory.Object,
            dbFactory.Object,
            auth,
            options.Object,
            NullLogger<TikTokShopAffiliateApiService>.Instance,
            cache);
    }

    private static OfficialCatalogSearchService CreateCatalogSearch(ITikTokShopAffiliateApiService tikTok)
    {
        var ml = new Mock<IMercadoLivreApiService>();
        ml.Setup(x => x.SearchCatalogAsync(It.IsAny<int>(), It.IsAny<MercadoLivreCatalogSearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OfficialCatalogChannelResult.Empty);
        var shopee = new Mock<IShopeeAffiliateOfferService>();
        shopee.Setup(x => x.SearchProductsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OfficialCatalogChannelResult.Empty);
        var amazon = new Mock<IAmazonPaApiService>();
        amazon.Setup(x => x.SearchProductsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OfficialCatalogChannelResult.Empty);
        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        factory.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CreateContext(Guid.NewGuid().ToString()));
        return new OfficialCatalogSearchService(
            ml.Object,
            shopee.Object,
            amazon.Object,
            factory.Object,
            NullLogger<OfficialCatalogSearchService>.Instance,
            tikTokShop: tikTok);
    }

    private static GlobalIntegrationService CreateGlobalService(AppDbContext context, IMarketplaceAuthService auth)
    {
        var mercadoLivre = new Mock<IOptionsMonitor<MercadoLivreIntegrationOptions>>();
        mercadoLivre.Setup(x => x.CurrentValue).Returns(new MercadoLivreIntegrationOptions { AppId = "123", SecretKey = "secret" });
        var tikTok = new Mock<IOptionsMonitor<TikTokShopIntegrationOptions>>();
        tikTok.Setup(x => x.CurrentValue).Returns(new TikTokShopIntegrationOptions { AppKey = "app-key", AppSecret = "app-secret" });
        return new GlobalIntegrationService(
            context,
            auth,
            Moq.Mock.Of<ISystemSettingsService>(),
            Moq.Mock.Of<IMasterAuditService>(),
            mercadoLivre.Object,
            NullLogger<GlobalIntegrationService>.Instance,
            tikTok.Object);
    }

    private static UserAccount User(string role) => new()
    {
        Id = 1,
        TenantId = Guid.NewGuid(),
        Name = "Master",
        Email = "master@ofertio.local",
        PasswordHash = "hash",
        Role = role
    };

    private static AppDbContext CreateContext(string name)
    {
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns<string>(s => s);
        encryption.Setup(e => e.Decrypt(It.IsAny<string>())).Returns<string>(s => s);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new AppDbContext(options, encryption.Object, new Ofertio.Database.MultiTenancy.NullCurrentTenantService());
    }
}
