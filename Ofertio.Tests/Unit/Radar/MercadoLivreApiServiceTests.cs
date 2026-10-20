using System.Net;
using System.Net.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Ofertio.Business.Integrations.Auth;
using Ofertio.Business.Integrations.MercadoLivre;
using Ofertio.Business.Service.Radar;
using Ofertio.Core.Entities;
using Ofertio.Core.Enums;
using Ofertio.Database;
using Ofertio.Infrastructure.Services.Radar;
using Ofertio.Util.Security;

namespace Ofertio.Tests.Unit.Radar;

public class MercadoLivreApiServiceTests
{
    private const string SearchPath = "/products/search";

    [Fact]
    public async Task SearchProductsAsync_ShouldAskForOAuthWhenTokenIsMissing()
    {
        var handler = new RoutingHandler();
        var sut = CreateSut(handler, new MercadoLivreIntegrationOptions());

        var result = await sut.SearchProductsAsync(1, "dell i7", 20);

        Assert.True(result.MissingCredentials);
        Assert.DoesNotContain(handler.Requests, request => request.Host == "api.mercadolibre.com");
    }

    [Fact]
    public async Task SearchProductsAsync_ShouldNotTreatAffiliateIdAsSearchCredential()
    {
        var handler = new RoutingHandler();
        var sut = CreateSut(
            handler,
            new MercadoLivreIntegrationOptions(),
            db => db.MarketplaceAccounts.Add(MercadoLivreAccount(trackingId: "14343296")));

        var result = await sut.SearchProductsAsync(1, "dell i7", 20, "14343296");

        Assert.True(result.MissingCredentials);
        Assert.Empty(result.Items);
        Assert.DoesNotContain(handler.Requests, request => request.Host == "api.mercadolibre.com");
    }

    [Fact]
    public async Task SearchProductsAsync_ShouldIgnoreUniversalLinkPlaceholderToken()
    {
        var handler = new RoutingHandler();
        var sut = CreateSut(
            handler,
            new MercadoLivreIntegrationOptions(),
            db =>
            {
                var account = MercadoLivreAccount(trackingId: "14343296");
                account.AccessToken = HomologMarketplaceAuth.StubAccessToken;
                account.ExpiresAt = DateTime.UtcNow.AddYears(10);
                db.MarketplaceAccounts.Add(account);
            });

        var result = await sut.SearchProductsAsync(1, "dell i7", 20);

        Assert.True(result.MissingCredentials);
        Assert.DoesNotContain(handler.Requests, request => request.Host == "api.mercadolibre.com");
    }

    [Fact]
    public async Task SearchProductsAsync_WithoutUserToken_ShouldUseVaultAppAccessToken()
    {
        var handler = new RoutingHandler { TokenBody = """{"access_token":"APP_USR-app","expires_in":21600}""" };
        var sut = CreateSut(
            handler,
            new MercadoLivreIntegrationOptions { AppId = "vault-app-" + Guid.NewGuid().ToString("N"), SecretKey = "vault-secret" },
            db => db.MarketplaceAccounts.Add(MercadoLivreAccount(trackingId: "14343296")));

        var result = await sut.SearchProductsAsync(1, "dell i7", 20);

        var tokenRequest = Assert.Single(handler.Requests, request => request.Path.Contains("oauth/token", StringComparison.Ordinal));
        Assert.Contains("grant_type=client_credentials", tokenRequest.Body, StringComparison.Ordinal);
        var search = Assert.Single(handler.Requests, request => request.Path == SearchPath);
        Assert.Equal("Bearer APP_USR-app", search.Authorization);
        var item = Assert.Single(result.Items);
        Assert.Contains("matt_tool=14343296", item.SourceUrl, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SearchProductsAsync_ShouldReuseCachedAppAccessToken()
    {
        var handler = new RoutingHandler { TokenBody = """{"access_token":"APP_USR-app","expires_in":21600}""" };
        var sut = CreateSut(
            handler,
            new MercadoLivreIntegrationOptions { AppId = "vault-app-" + Guid.NewGuid().ToString("N"), SecretKey = "vault-secret" });

        await sut.SearchProductsAsync(1, "dell i7", 20);
        await sut.SearchProductsAsync(1, "iphone", 20);

        Assert.Single(handler.Requests, request => request.Path.Contains("oauth/token", StringComparison.Ordinal));
        Assert.Equal(2, handler.Requests.Count(request => request.Path == SearchPath));
    }

    [Fact]
    public async Task SearchProductsAsync_ShouldRefreshAndPersistRotatedToken_WhenAccessTokenExpired()
    {
        var dbName = Guid.NewGuid().ToString();
        var handler = new RoutingHandler { TokenBody = """{"access_token":"APP_USR-new","refresh_token":"TG-new","expires_in":21600}""" };
        var sut = CreateSut(
            handler,
            new MercadoLivreIntegrationOptions { AppId = "123", SecretKey = "secret" },
            db =>
            {
                var account = MercadoLivreAccount(trackingId: null);
                account.AccessToken = "APP_USR-old";
                account.RefreshToken = "TG-old";
                account.ExpiresAt = DateTime.UtcNow.AddHours(-1);
                db.MarketplaceAccounts.Add(account);
            },
            dbName);

        var result = await sut.SearchProductsAsync(1, "dell i7", 20);

        Assert.Single(result.Items);
        var search = Assert.Single(handler.Requests, request => request.Path == SearchPath);
        Assert.Equal("Bearer APP_USR-new", search.Authorization);
        await using var verify = CreateDbContext(dbName);
        var stored = await verify.MarketplaceAccounts.IgnoreQueryFilters().SingleAsync();
        Assert.Equal("APP_USR-new", stored.AccessToken);
        Assert.Equal("TG-new", stored.RefreshToken);
        Assert.True(stored.ExpiresAt > DateTime.UtcNow.AddHours(5));
    }

    [Fact]
    public async Task SearchProductsAsync_ShouldSendBearerTokenOnlyInHeader()
    {
        var handler = new RoutingHandler();
        var sut = CreateSut(handler, new MercadoLivreIntegrationOptions { AccessToken = "ml-app-token" });

        var result = await sut.SearchProductsAsync(1, "dell i7", 20);

        var search = Assert.Single(handler.Requests, request => request.Path == SearchPath);
        Assert.Equal("Bearer ml-app-token", search.Authorization);
        Assert.Equal("https://api.mercadolibre.com/products/search?status=active&site_id=MLB&q=dell%20i7&limit=20", search.Url);
        Assert.All(handler.Requests, request => Assert.DoesNotContain("access_token", request.Url, StringComparison.Ordinal));
        Assert.Equal("Dell i7", Assert.Single(result.Items).ProductName);
    }

    [Fact]
    public async Task SearchProductsAsync_ShouldInjectMattToolWhenOAuthSearchSucceeds()
    {
        var handler = new RoutingHandler();
        var sut = CreateSut(
            handler,
            new MercadoLivreIntegrationOptions { AccessToken = "ml-app-token" },
            db =>
            {
                var account = MercadoLivreAccount(trackingId: "14343296");
                account.AccessToken = "ml-app-token";
                account.ExpiresAt = DateTime.UtcNow.AddHours(4);
                db.MarketplaceAccounts.Add(account);
            });

        var result = await sut.SearchProductsAsync(1, "dell i7", 20, "14343296");

        Assert.False(result.MissingCredentials);
        var item = Assert.Single(result.Items);
        Assert.Contains("matt_tool=14343296", item.SourceUrl, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Api", item.Source);
    }

    [Fact]
    public async Task SearchProductsAsync_OnForbidden_ShouldExposeForbiddenMessageWithoutScrapingLista()
    {
        var handler = new RoutingHandler { SearchStatus = HttpStatusCode.Forbidden, SearchBody = """{"message":"forbidden"}""" };
        var sut = CreateSut(
            handler,
            new MercadoLivreIntegrationOptions { AccessToken = "app-token" },
            db => db.MarketplaceAccounts.Add(MercadoLivreAccount(trackingId: "14343296")));

        var result = await sut.SearchProductsAsync(1, "dell i7");

        Assert.Empty(result.Items);
        Assert.Equal(OfficialCatalogSearchRules.MercadoLivreSearchForbiddenMessage, result.ErrorMessage);
        var search = Assert.Single(handler.Requests);
        Assert.Equal("Bearer app-token", search.Authorization);
    }

    [Fact]
    public async Task SearchProductsAsync_OnUserScopeError_ShouldExposeForbiddenMessage()
    {
        var handler = new RoutingHandler
        {
            SearchStatus = HttpStatusCode.BadRequest,
            SearchBody = """{"message":"missing required scopes","error":"unauthorized_scopes"}"""
        };
        var sut = CreateSut(handler, new MercadoLivreIntegrationOptions { AccessToken = "app-token" });

        var result = await sut.SearchProductsAsync(1, "dell i7");

        Assert.Empty(result.Items);
        Assert.Equal(OfficialCatalogSearchRules.MercadoLivreSearchForbiddenMessage, result.ErrorMessage);
        Assert.DoesNotContain(handler.Requests, request => request.Host.StartsWith("lista.", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SearchProductsAsync_OnForbiddenWithStaticToken_ShouldExposeForbiddenMessage()
    {
        var handler = new RoutingHandler { SearchStatus = HttpStatusCode.Forbidden };
        var sut = CreateSut(handler, new MercadoLivreIntegrationOptions { AccessToken = "expired" });

        var result = await sut.SearchProductsAsync(1, "dell i7");

        Assert.False(result.MissingCredentials);
        Assert.Empty(result.Items);
        Assert.Equal(OfficialCatalogSearchRules.MercadoLivreSearchForbiddenMessage, result.ErrorMessage);
    }

    [Fact]
    public async Task SearchProductsAsync_OnForbiddenWithFreshAccountToken_ShouldRefreshPersistAndRetry()
    {
        var dbName = Guid.NewGuid().ToString();
        var handler = new RoutingHandler { TokenBody = """{"access_token":"APP_USR-renewed","refresh_token":"TG-renewed","expires_in":21600}""" };
        handler.RefusedBearers.Add("APP_USR-stale");
        var sut = CreateSut(
            handler,
            new MercadoLivreIntegrationOptions { AppId = "vault-app", SecretKey = "vault-secret" },
            db =>
            {
                var account = MercadoLivreAccount(trackingId: "14343296");
                account.FriendlyName = "@Achadinhos de Aaz";
                account.AccessToken = "APP_USR-stale";
                account.RefreshToken = "TG-stale";
                account.AppKey = "account-app";
                account.AppSecret = "account-secret";
                account.ExpiresAt = DateTime.UtcNow.AddHours(4);
                db.MarketplaceAccounts.Add(account);
            },
            dbName);

        var result = await sut.SearchProductsAsync(1, "dell i7", 20);

        var searches = handler.Requests.Where(request => request.Path == SearchPath).ToList();
        Assert.Equal(["Bearer APP_USR-stale", "Bearer APP_USR-renewed"], searches.Select(search => search.Authorization));
        var tokenRequest = Assert.Single(handler.Requests, request => request.Path.Contains("oauth/token", StringComparison.Ordinal));
        Assert.Contains("grant_type=refresh_token", tokenRequest.Body, StringComparison.Ordinal);
        Assert.Contains("refresh_token=TG-stale", tokenRequest.Body, StringComparison.Ordinal);
        Assert.Contains("client_id=vault-app", tokenRequest.Body, StringComparison.Ordinal);
        Assert.Equal("Api", Assert.Single(result.Items).Source);
        Assert.DoesNotContain(handler.Requests, request => request.Host.StartsWith("lista.", StringComparison.OrdinalIgnoreCase));
        await using var verify = CreateDbContext(dbName);
        var stored = await verify.MarketplaceAccounts.IgnoreQueryFilters().SingleAsync();
        Assert.Equal("APP_USR-renewed", stored.AccessToken);
        Assert.Equal("TG-renewed", stored.RefreshToken);
    }

    [Fact]
    public async Task SearchProductsAsync_OnForbiddenAndRefreshFailure_ShouldExposeForbiddenMessage()
    {
        var handler = new RoutingHandler();
        handler.RefusedBearers.Add("APP_USR-stale");
        var sut = CreateSut(
            handler,
            new MercadoLivreIntegrationOptions { AppId = "vault-app", SecretKey = "vault-secret" },
            db =>
            {
                var account = MercadoLivreAccount(trackingId: "14343296");
                account.AccessToken = "APP_USR-stale";
                account.RefreshToken = "TG-stale";
                account.ExpiresAt = DateTime.UtcNow.AddHours(4);
                db.MarketplaceAccounts.Add(account);
            });

        var result = await sut.SearchProductsAsync(1, "dell i7", 20);

        Assert.Single(handler.Requests, request => request.Path == SearchPath);
        Assert.Single(handler.Requests, request => request.Path.Contains("oauth/token", StringComparison.Ordinal));
        Assert.Empty(result.Items);
        Assert.Equal(OfficialCatalogSearchRules.MercadoLivreSearchForbiddenMessage, result.ErrorMessage);
        Assert.DoesNotContain(handler.Requests, request => request.Host.StartsWith("lista.", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SearchProductsAsync_ShouldUseAccountFromCurrentUserTenant()
    {
        var tenantId = Guid.NewGuid();
        var handler = new RoutingHandler();
        var sut = CreateSut(
            handler,
            new MercadoLivreIntegrationOptions(),
            db =>
            {
                db.UserAccounts.Add(new UserAccount
                {
                    Id = 1,
                    TenantId = tenantId,
                    Name = "Rony",
                    Email = "rony@ofertio.dev",
                    PasswordHash = "hash"
                });
                var foreign = MercadoLivreAccount(trackingId: null);
                foreign.UserId = null;
                foreign.AccessToken = "APP_USR-other-tenant";
                foreign.ExpiresAt = DateTime.UtcNow.AddYears(1);
                var own = MercadoLivreAccount(trackingId: null);
                own.TenantId = tenantId;
                own.AccessToken = "APP_USR-own";
                own.ExpiresAt = DateTime.UtcNow.AddHours(4);
                db.MarketplaceAccounts.AddRange(foreign, own);
            });

        await sut.SearchProductsAsync(1, "dell i7", 20);

        Assert.All(handler.Requests, request => Assert.Equal("Bearer APP_USR-own", request.Authorization));
        Assert.Single(handler.Requests, request => request.Path == SearchPath);
    }

    [Fact]
    public async Task SearchProductsAsync_ShouldUseConnectedAccountBearer_NeverAnonymousSearch()
    {
        var handler = new RoutingHandler();
        var sut = CreateSut(
            handler,
            new MercadoLivreIntegrationOptions { AppId = "vault-app", SecretKey = "vault-secret" },
            db =>
            {
                var account = MercadoLivreAccount(trackingId: "97303724");
                account.AccessToken = "APP_USR-valid";
                account.ExpiresAt = DateTime.UtcNow.AddHours(4);
                db.MarketplaceAccounts.Add(account);
            });

        var result = await sut.SearchProductsAsync(1, "dell i7");

        Assert.Null(result.ErrorMessage);
        var item = Assert.Single(result.Items);
        Assert.Equal("Api", item.Source);
        Assert.Equal("Dell i7", item.ProductName);
        Assert.Equal(10m, item.Price);
        Assert.Equal("https://http2.mlstatic.com/t.jpg", item.ImageUrl);
        Assert.StartsWith("https://www.mercadolivre.com.br/dell-i7/p/MLB1", item.SourceUrl, StringComparison.Ordinal);
        Assert.Contains("matt_tool=97303724", item.SourceUrl, StringComparison.OrdinalIgnoreCase);
        Assert.Equal([SearchPath, "/products/MLB1"], handler.Requests.Select(request => request.Path));
        Assert.All(handler.Requests, request => Assert.Equal("Bearer APP_USR-valid", request.Authorization));
        Assert.DoesNotContain(handler.Requests, request => request.Path.Contains("sites/MLB/search", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SearchProductsAsync_ShouldSkipCatalogProductsWithoutBuyBoxWinner()
    {
        var handler = new RoutingHandler
        {
            SearchBody = """{"results":[{"id":"MLB1"},{"id":"MLB2"},{"id":"MLB3"}]}"""
        };
        handler.PathResponses["/products/MLB2"] = (HttpStatusCode.OK, """{"id":"MLB2","name":"Sem oferta","buy_box_winner":null}""");
        handler.PathResponses["/products/MLB3"] = (HttpStatusCode.Forbidden, """{"message":"forbidden"}""");
        var sut = CreateSut(handler, new MercadoLivreIntegrationOptions { AccessToken = "APP_USR-static" });

        var result = await sut.SearchProductsAsync(1, "dell i7", 20);

        Assert.Null(result.ErrorMessage);
        Assert.Equal("MLB1", Assert.Single(result.Items).ProductId);
        Assert.Equal(5, handler.Requests.Count);
        Assert.Contains(handler.Requests, request => request.Path == "/products/MLB2/items");
    }

    [Fact]
    public async Task SearchProductsAsync_WithoutBuyBox_ShouldUsePriceRangeItemsAndChildProducts()
    {
        var handler = new RoutingHandler
        {
            SearchBody = """{"results":[{"id":"MLB10"},{"id":"MLB20"},{"id":"MLB30"}]}"""
        };
        handler.PathResponses["/products/MLB10"] = (
            HttpStatusCode.OK,
            """{"id":"MLB10","name":"Faixa","buy_box_winner":null,"buy_box_winner_price_range":{"min":{"price":3500},"max":{"price":4200}}}""");
        handler.PathResponses["/products/MLB20"] = (HttpStatusCode.OK, """{"id":"MLB20","name":"Sem disputa","buy_box_winner":null}""");
        handler.PathResponses["/products/MLB20/items"] = (
            HttpStatusCode.OK,
            """{"results":[{"item_id":"MLB201","price":4999},{"item_id":"MLB202","price":4599.9,"shipping":{"free_shipping":true}}]}""");
        handler.PathResponses["/products/MLB30"] = (HttpStatusCode.OK, """{"id":"MLB30","name":"Dell Inspiron (pai)","buy_box_winner":null,"children_ids":["MLB31","MLB32"]}""");
        handler.PathResponses["/products/MLB30/items"] = (HttpStatusCode.NotFound, """{"message":"not found"}""");
        handler.PathResponses["/products/MLB31"] = (
            HttpStatusCode.OK,
            """{"id":"MLB31","name":"Dell Inspiron i7 16GB","permalink":"https://www.mercadolivre.com.br/dell/p/MLB31","buy_box_winner":{"item_id":"MLB311","price":5299}}""");
        var sut = CreateSut(handler, new MercadoLivreIntegrationOptions { AccessToken = "APP_USR-static" });

        var result = await sut.SearchProductsAsync(1, "dell i7", 20);

        Assert.Equal(["MLB10", "MLB20", "MLB31"], result.Items.Select(item => item.ProductId));
        Assert.Equal([3500m, 4599.9m, 5299m], result.Items.Select(item => item.Price!.Value));
        Assert.Equal("Frete grátis", result.Items[1].Shipping);
        Assert.DoesNotContain(handler.Requests, request => request.Path == "/products/MLB32");
        Assert.All(handler.Requests, request => Assert.Equal("Bearer APP_USR-static", request.Authorization));
    }

    [Fact]
    public async Task SearchProductsAsync_WithoutAnyToken_ShouldNotCallPublicSearchOrLista()
    {
        var handler = new RoutingHandler();
        var sut = CreateSut(
            handler,
            new MercadoLivreIntegrationOptions(),
            db => db.MarketplaceAccounts.Add(MercadoLivreAccount(trackingId: "97303724")));

        var result = await sut.SearchProductsAsync(1, "dell i7");

        Assert.True(result.MissingCredentials);
        Assert.Empty(result.Items);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task GetItemAsync_ShouldCallOfficialItemsEndpointWithConnectedAccountBearer()
    {
        var handler = new RoutingHandler();
        var sut = CreateSut(
            handler,
            new MercadoLivreIntegrationOptions(),
            db =>
            {
                var account = MercadoLivreAccount(trackingId: null);
                account.AccessToken = "APP_USR-loja";
                account.ExpiresAt = DateTime.UtcNow.AddHours(4);
                db.MarketplaceAccounts.Add(account);
            });

        var item = await sut.GetItemAsync(1, "MLB123");

        var request = Assert.Single(handler.Requests);
        Assert.Equal("https://api.mercadolibre.com/items/MLB123", request.Url);
        Assert.Equal("Bearer APP_USR-loja", request.Authorization);
        Assert.NotNull(item);
        Assert.Equal("Notebook Dell", item!.ProductName);
        Assert.Equal(3999.9m, item.Price);
        Assert.True(item.IsAvailable);
    }

    [Fact]
    public async Task GetItemAsync_ShouldRefreshExpiredTokenBeforeCallingItems()
    {
        var dbName = Guid.NewGuid().ToString();
        var handler = new RoutingHandler { TokenBody = """{"access_token":"APP_USR-new","refresh_token":"TG-new","expires_in":21600}""" };
        var sut = CreateSut(
            handler,
            new MercadoLivreIntegrationOptions { AppId = "123", SecretKey = "secret" },
            db =>
            {
                var account = MercadoLivreAccount(trackingId: null);
                account.AccessToken = "APP_USR-old";
                account.RefreshToken = "TG-old";
                account.ExpiresAt = DateTime.UtcNow.AddHours(-1);
                db.MarketplaceAccounts.Add(account);
            },
            dbName);

        var item = await sut.GetItemAsync(1, "MLB123");

        Assert.NotNull(item);
        var tokenRequest = Assert.Single(handler.Requests, request => request.Path.Contains("oauth/token", StringComparison.Ordinal));
        Assert.Contains("grant_type=refresh_token", tokenRequest.Body, StringComparison.Ordinal);
        var itemRequest = Assert.Single(handler.Requests, request => request.Path.StartsWith("/items/", StringComparison.Ordinal));
        Assert.Equal("Bearer APP_USR-new", itemRequest.Authorization);
        await using var verify = CreateDbContext(dbName);
        Assert.Equal("APP_USR-new", (await verify.MarketplaceAccounts.IgnoreQueryFilters().SingleAsync()).AccessToken);
    }

    [Fact]
    public async Task GetItemAsync_OnForbidden_ShouldRefreshAccountTokenAndRetry()
    {
        var handler = new RoutingHandler { TokenBody = """{"access_token":"APP_USR-renewed","refresh_token":"TG-renewed","expires_in":21600}""" };
        handler.RefusedBearers.Add("APP_USR-stale");
        var sut = CreateSut(
            handler,
            new MercadoLivreIntegrationOptions { AppId = "vault-app", SecretKey = "vault-secret" },
            db =>
            {
                var account = MercadoLivreAccount(trackingId: null);
                account.AccessToken = "APP_USR-stale";
                account.RefreshToken = "TG-stale";
                account.ExpiresAt = DateTime.UtcNow.AddHours(4);
                db.MarketplaceAccounts.Add(account);
            });

        var item = await sut.GetItemAsync(1, "MLB123");

        Assert.NotNull(item);
        var items = handler.Requests.Where(request => request.Path.StartsWith("/items/", StringComparison.Ordinal)).ToList();
        Assert.Equal(["Bearer APP_USR-stale", "Bearer APP_USR-renewed"], items.Select(request => request.Authorization));
    }

    [Fact]
    public async Task GetItemAsync_WithoutConnectedAccountOrAppCredentials_ShouldNotCallItems()
    {
        var handler = new RoutingHandler();
        var sut = CreateSut(handler, new MercadoLivreIntegrationOptions());

        var item = await sut.GetItemAsync(1, "MLB123");

        Assert.Null(item);
        Assert.DoesNotContain(handler.Requests, request => request.Path.StartsWith("/items/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetItemAsync_WithCatalogId_ShouldFollowBuyBoxWinnerThroughProductsEndpoint()
    {
        var handler = new RoutingHandler();
        handler.PathResponses["/items/MLB19700000"] = (HttpStatusCode.NotFound, """{"message":"Item with id MLB19700000 not found","error":"not_found"}""");
        handler.PathResponses["/products/MLB19700000"] = (
            HttpStatusCode.OK,
            """{"id":"MLB19700000","name":"Fone Catálogo","pictures":[{"url":"https://http2.mlstatic.com/p.jpg"}],"buy_box_winner":{"item_id":"MLB123","price":3999.9}}""");
        var sut = CreateSut(handler, new MercadoLivreIntegrationOptions { AccessToken = "APP_USR-static" });

        var item = await sut.GetItemAsync(0, "MLB19700000");

        Assert.NotNull(item);
        Assert.Equal("Notebook Dell", item!.ProductName);
        Assert.True(item.IsAvailable);
        Assert.Equal(
            ["/items/MLB19700000", "/products/MLB19700000", "/items/MLB123"],
            handler.Requests.Select(request => request.Path));
        Assert.All(handler.Requests, request => Assert.Equal("Bearer APP_USR-static", request.Authorization));
    }

    [Fact]
    public async Task GetItemAsync_WithoutUser_ShouldNotUseAnyTenantAccountToken()
    {
        var handler = new RoutingHandler();
        var sut = CreateSut(
            handler,
            new MercadoLivreIntegrationOptions { AccessToken = "APP_USR-static" },
            db =>
            {
                var account = MercadoLivreAccount(trackingId: null);
                account.AccessToken = "APP_USR-loja";
                account.ExpiresAt = DateTime.UtcNow.AddHours(4);
                db.MarketplaceAccounts.Add(account);
            });

        await sut.GetItemAsync(0, "MLB123");

        var request = Assert.Single(handler.Requests);
        Assert.Equal("Bearer APP_USR-static", request.Authorization);
    }

    private static MarketplaceAccount MercadoLivreAccount(string? trackingId) => new()
    {
        TenantId = Guid.NewGuid(),
        UserId = "1",
        MarketplaceType = MarketplaceType.MercadoLivre,
        TrackingId = trackingId,
        IsActive = true
    };

    private static MercadoLivreApiService CreateSut(
        HttpMessageHandler handler,
        MercadoLivreIntegrationOptions options,
        Action<AppDbContext>? seed = null,
        string? dbName = null)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.mercadolibre.com/") };
        dbName ??= Guid.NewGuid().ToString();
        if (seed is not null)
        {
            using var seedContext = CreateDbContext(dbName);
            seed(seedContext);
            seedContext.SaveChanges();
        }

        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        factory.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CreateDbContext(dbName));
        var monitor = new Mock<IOptionsMonitor<MercadoLivreIntegrationOptions>>();
        monitor.Setup(x => x.CurrentValue).Returns(options);
        return new MercadoLivreApiService(
            http,
            monitor.Object,
            factory.Object,
            NullLogger<MercadoLivreApiService>.Instance);
    }

    private static AppDbContext CreateDbContext(string? name = null)
    {
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns<string>(s => s);
        encryption.Setup(e => e.Decrypt(It.IsAny<string>())).Returns<string>(s => s);
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name ?? Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(dbOptions, encryption.Object, new Ofertio.Database.MultiTenancy.NullCurrentTenantService());
    }

    private sealed record CapturedRequest(string Url, string Host, string Path, string? Authorization, string Body);

    private sealed class RoutingHandler : HttpMessageHandler
    {
        public List<CapturedRequest> Requests { get; } = [];

        public HttpStatusCode SearchStatus { get; set; } = HttpStatusCode.OK;

        public string SearchBody { get; set; } =
            """{"keywords":"dell i7","results":[{"id":"MLB1","name":"Dell i7"}]}""";

        public string ProductBody { get; set; } =
            """{"id":"MLB1","name":"Dell i7","permalink":"https://www.mercadolivre.com.br/dell-i7/p/MLB1","pictures":[{"url":"https://http2.mlstatic.com/t.jpg"}],"buy_box_winner":{"item_id":"MLB111","price":10}}""";

        public string TokenBody { get; set; } = """{"error":"invalid_client"}""";

        public string ItemBody { get; set; } =
            """{"id":"MLB123","title":"Notebook Dell","price":3999.9,"status":"active","available_quantity":5,"secure_thumbnail":"https://http2.mlstatic.com/i.jpg"}""";

        public HashSet<string> RefusedBearers { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, (HttpStatusCode Status, string Body)> PathResponses { get; } = new(StringComparer.Ordinal);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var uri = request.RequestUri!;
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            lock (Requests)
            {
                Requests.Add(new CapturedRequest(
                    uri.AbsoluteUri,
                    uri.Host,
                    uri.AbsolutePath,
                    request.Headers.Authorization?.ToString(),
                    body));
            }

            var (status, content) = PathResponses.TryGetValue(uri.AbsolutePath, out var routed)
                ? routed
                : uri.AbsolutePath.Contains("oauth/token", StringComparison.Ordinal)
                ? (TokenBody.Contains("access_token", StringComparison.Ordinal) ? HttpStatusCode.OK : HttpStatusCode.BadRequest, TokenBody)
                    : RefusedBearers.Contains(request.Headers.Authorization?.Parameter ?? string.Empty)
                        ? (HttpStatusCode.Forbidden, """{"message":"forbidden"}""")
                        : uri.AbsolutePath.StartsWith("/items/", StringComparison.Ordinal)
                            ? (HttpStatusCode.OK, ItemBody)
                            : uri.AbsolutePath.StartsWith("/products/", StringComparison.Ordinal) && uri.AbsolutePath != SearchPath
                                ? (HttpStatusCode.OK, ProductBody)
                                : (SearchStatus, SearchBody);
            return new HttpResponseMessage(status)
            {
                RequestMessage = request,
                Content = new StringContent(content)
            };
        }
    }
}
