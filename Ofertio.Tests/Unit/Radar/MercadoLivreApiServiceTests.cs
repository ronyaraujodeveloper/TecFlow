using System.Net;
using System.Net.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Ofertio.Business.Dto;
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
        handler.PathResponses["/products/MLB30"] = (
            HttpStatusCode.OK,
            """{"id":"MLB30","name":"Dell Inspiron (pai)","domain_id":"MLB-NOTEBOOKS","buy_box_winner":null,"children_ids":["MLB31","MLB32","MLB33","MLB34","MLB35"]}""");
        handler.PathResponses["/products/MLB30/items"] = (HttpStatusCode.NotFound, """{"message":"not found"}""");
        handler.PathResponses["/products/MLB31"] = (
            HttpStatusCode.OK,
            """{"id":"MLB31","name":"Dell Inspiron i7 16GB","permalink":"https://www.mercadolivre.com.br/dell/p/MLB31","buy_box_winner":{"item_id":"MLB311","price":5299}}""");
        handler.PathResponses["/products/MLB32"] = (
            HttpStatusCode.OK,
            """{"id":"MLB32","name":"Dell Inspiron i5 8GB","domain_id":"MLB-NOTEBOOKS","buy_box_winner":{"item_id":"MLB321","price":3899}}""");
        handler.PathResponses["/products/MLB33"] = (HttpStatusCode.OK, """{"id":"MLB33","name":"Sem oferta","buy_box_winner":null}""");
        handler.PathResponses["/products/MLB33/items"] = (HttpStatusCode.NotFound, """{"message":"No winners found"}""");
        handler.PathResponses["/products/MLB34"] = (HttpStatusCode.OK, """{"id":"MLB34","name":"Dell Inspiron i3","buy_box_winner":{"item_id":"MLB341","price":2999}}""");
        var sut = CreateSut(handler, new MercadoLivreIntegrationOptions { AccessToken = "APP_USR-static" });

        var result = await sut.SearchProductsAsync(1, "dell i7", 20);

        Assert.Equal(["MLB10", "MLB20", "MLB31", "MLB32", "MLB34"], result.Items.Select(item => item.ProductId));
        Assert.Equal([3500m, 4599.9m, 5299m, 3899m, 2999m], result.Items.Select(item => item.Price!.Value));
        Assert.Equal("Frete grátis", result.Items[1].Shipping);
        Assert.Equal("MLB-NOTEBOOKS", result.Items[2].DomainId);
        Assert.DoesNotContain(handler.Requests, request => request.Path == "/products/MLB35");
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

    [Fact]
    public async Task SearchCatalogAsync_ShouldScanPagesUntilTargetAndExposeNextOffsetAndCategories()
    {
        var offers = new HashSet<string>(Enumerable.Range(20, 5).Select(index => $"MLB{index}"));
        var handler = new RoutingHandler
        {
            Responder = uri => uri.AbsolutePath switch
            {
                SearchPath => (HttpStatusCode.OK, SearchPage(Range(QueryValue(uri, "offset") is { } offset ? int.Parse(offset) : 0, 20), 200, "MLB-GAMING_HEADSETS")),
                "/sites/MLB/domain_discovery/search" => (HttpStatusCode.OK, """[{"domain_id":"MLB-NOTEBOOKS","domain_name":"Notebooks"}]"""),
                _ => CatalogProduct(uri, id => offers.Contains(id) ? (100m, true) : null)
            }
        };
        var sut = CreateSut(handler, new MercadoLivreIntegrationOptions { AccessToken = "APP_USR-static" });

        var result = await sut.SearchCatalogAsync(1, new MercadoLivreCatalogSearchRequest { Query = "dell", TargetOffers = 5 });

        Assert.Equal(5, result.Items.Count);
        Assert.Equal(40, result.ScannedCount);
        Assert.Equal(40, result.NextOffset);
        Assert.Equal("dell", result.EffectiveQuery);
        Assert.Equal(
            ["https://api.mercadolibre.com/products/search?status=active&site_id=MLB&q=dell&limit=20",
             "https://api.mercadolibre.com/products/search?status=active&site_id=MLB&q=dell&limit=20&offset=20"],
            handler.Requests.Where(request => request.Path == SearchPath).Select(request => request.Url));
        Assert.Equal(["MLB-NOTEBOOKS", "MLB-GAMING_HEADSETS"], result.Categories.Select(category => category.Id));
        Assert.Equal(["Notebooks", "Gaming headsets"], result.Categories.Select(category => category.Name));
    }

    [Fact]
    public async Task SearchCatalogAsync_WhenTermHasFewOffers_ShouldKeepScanningUpToTheFullBudget()
    {
        var handler = new RoutingHandler
        {
            Responder = uri => uri.AbsolutePath == SearchPath
                ? (HttpStatusCode.OK, SearchPage(Range(QueryValue(uri, "offset") is { } offset ? int.Parse(offset) : 0, 20), 500))
                : CatalogProduct(uri, id => id == "MLB0" ? (4000m, false) : null)
        };
        var sut = CreateSut(handler, new MercadoLivreIntegrationOptions { AccessToken = "APP_USR-static" });

        var result = await sut.SearchCatalogAsync(1, new MercadoLivreCatalogSearchRequest
        {
            Query = "dell inspiron",
            DomainId = "MLB-NOTEBOOKS",
            IncludeCategories = false
        });

        Assert.Equal("MLB0", Assert.Single(result.Items).ProductId);
        Assert.Equal(OfficialCatalogSearchRules.MercadoLivreMaxScannedProducts, result.ScannedCount);
        Assert.Equal(OfficialCatalogSearchRules.MercadoLivreMaxScannedProducts, result.NextOffset);
        Assert.Equal("dell inspiron", result.EffectiveQuery);
        Assert.All(
            handler.Requests.Where(request => request.Path == SearchPath),
            request => Assert.Equal("dell inspiron", QueryValue(new Uri(request.Url), "q")));
    }

    [Fact]
    public async Task SearchCatalogAsync_ShouldApplyPriceRangeAndFreeShippingDuringScan()
    {
        var prices = new Dictionary<string, (decimal Price, bool Free)>
        {
            ["MLB1"] = (50m, true),
            ["MLB2"] = (150m, false),
            ["MLB3"] = (300m, true)
        };
        var handler = new RoutingHandler
        {
            Responder = uri => uri.AbsolutePath == SearchPath
                ? (HttpStatusCode.OK, SearchPage(["MLB1", "MLB2", "MLB3"], 3))
                : CatalogProduct(uri, id => prices.TryGetValue(id, out var offer) ? offer : null)
        };
        var sut = CreateSut(handler, new MercadoLivreIntegrationOptions { AccessToken = "APP_USR-static" });

        var result = await sut.SearchCatalogAsync(1, new MercadoLivreCatalogSearchRequest
        {
            Query = "fone",
            MinPrice = 100m,
            MaxPrice = 400m,
            FreeShippingOnly = true,
            IncludeCategories = false
        });

        Assert.Equal("MLB3", Assert.Single(result.Items).ProductId);
        Assert.Equal(3, result.ScannedCount);
        Assert.Null(result.NextOffset);
    }

    [Fact]
    public async Task SearchCatalogAsync_WhenTermHasNoOffers_ShouldRetryWithShorterQuery()
    {
        var handler = new RoutingHandler
        {
            Responder = uri => uri.AbsolutePath == SearchPath
                ? (HttpStatusCode.OK, QueryValue(uri, "q") == "dell inspiron"
                    ? SearchPage(["MLB9"], 1)
                    : SearchPage(["MLB1", "MLB2"], 2))
                : CatalogProduct(uri, id => id == "MLB9" ? (3500m, false) : null)
        };
        var sut = CreateSut(handler, new MercadoLivreIntegrationOptions { AccessToken = "APP_USR-static" });

        var result = await sut.SearchCatalogAsync(1, new MercadoLivreCatalogSearchRequest
        {
            Query = "dell inspiron i7 16gb",
            IncludeCategories = false
        });

        Assert.Equal("MLB9", Assert.Single(result.Items).ProductId);
        Assert.Equal("dell inspiron", result.EffectiveQuery);
        Assert.Equal(5, result.ScannedCount);
        Assert.Equal(
            ["dell inspiron i7 16gb", "dell inspiron i7", "dell inspiron"],
            handler.Requests.Where(request => request.Path == SearchPath).Select(request => QueryValue(new Uri(request.Url), "q")));
    }

    [Fact]
    public async Task SearchCatalogAsync_WithDomainAndOffset_ShouldSendFiltersAndNotShortenQuery()
    {
        var handler = new RoutingHandler
        {
            Responder = uri => uri.AbsolutePath == SearchPath
                ? (HttpStatusCode.OK, SearchPage([], 0))
                : null
        };
        var sut = CreateSut(handler, new MercadoLivreIntegrationOptions { AccessToken = "APP_USR-static" });

        var result = await sut.SearchCatalogAsync(1, new MercadoLivreCatalogSearchRequest
        {
            Query = "dell inspiron",
            Offset = 40,
            DomainId = "MLB-NOTEBOOKS"
        });

        Assert.Empty(result.Items);
        var search = Assert.Single(handler.Requests);
        Assert.Equal(
            "https://api.mercadolibre.com/products/search?status=active&site_id=MLB&q=dell%20inspiron&limit=20&offset=40&domain_id=MLB-NOTEBOOKS",
            search.Url);
        Assert.Null(result.NextOffset);
    }

    [Fact]
    public async Task SearchCatalogAsync_ShouldEnrichOffersWithSalesSellerReviewsAndBestSellerRanking()
    {
        var handler = new RoutingHandler
        {
            Responder = uri => uri.AbsolutePath switch
            {
                SearchPath => (HttpStatusCode.OK, SearchPage(["MLB1"], 1)),
                "/products/MLB1" => (HttpStatusCode.OK, """{"id":"MLB1","name":"Dell Inspiron","buy_box_winner":{"item_id":"MLB111","price":3999,"seller_id":42,"category_id":"MLB1652","shipping":{"free_shipping":true}}}"""),
                "/items" => (HttpStatusCode.OK, """[{"code":200,"body":{"id":"MLB111","sold_quantity":321,"warranty":"12 meses","condition":"new"}}]"""),
                "/users/42" => (HttpStatusCode.OK, """{"id":42,"nickname":"LOJA.DELL","seller_reputation":{"level_id":"5_green","power_seller_status":"gold","transactions":{"completed":900,"ratings":{"positive":0.99}}}}"""),
                "/reviews/item/MLB111" => (HttpStatusCode.OK, """{"paging":{"total":87},"rating_average":4.6}"""),
                "/highlights/MLB/category/MLB1652" => (HttpStatusCode.OK, """{"content":[{"id":"MLB9","position":1,"type":"PRODUCT"},{"id":"MLB1","position":3,"type":"PRODUCT"}]}"""),
                _ => null
            }
        };
        var sut = CreateSut(handler, new MercadoLivreIntegrationOptions { AccessToken = "APP_USR-static" });

        var result = await sut.SearchCatalogAsync(1, new MercadoLivreCatalogSearchRequest { Query = "dell inspiron", IncludeCategories = false });

        var item = Assert.Single(result.Items);
        Assert.Equal(321, item.SoldQuantity);
        Assert.Equal("LOJA.DELL", item.SellerName);
        Assert.Equal("5_green", item.SellerReputationLevel);
        Assert.Equal("gold", item.SellerPowerStatus);
        Assert.Equal(900, item.SellerCompletedSales);
        Assert.Equal(0.99m, item.SellerPositiveRate);
        Assert.Equal(4.6m, item.RatingAverage);
        Assert.Equal(87, item.RatingCount);
        Assert.Equal(3, item.BestSellerPosition);
        Assert.Equal("12 meses", item.Warranty);
        var multiget = Assert.Single(handler.Requests, request => request.Path == "/items");
        Assert.Contains("ids=MLB111", multiget.Url, StringComparison.Ordinal);
        Assert.Contains(handler.Requests, request => request.Url.EndsWith("/reviews/item/MLB111?catalog_product_id=MLB1", StringComparison.Ordinal));
        Assert.All(handler.Requests, request => Assert.Equal("Bearer APP_USR-static", request.Authorization));
    }

    [Fact]
    public async Task SearchProductsAsync_ShouldNotCallMetricsEndpoints()
    {
        var handler = new RoutingHandler();
        var sut = CreateSut(handler, new MercadoLivreIntegrationOptions { AccessToken = "APP_USR-static" });

        await sut.SearchProductsAsync(1, "dell i7", 20);

        Assert.DoesNotContain(handler.Requests, request =>
            request.Path == "/items" || request.Path.StartsWith("/users/", StringComparison.Ordinal)
            || request.Path.StartsWith("/reviews/", StringComparison.Ordinal) || request.Path.StartsWith("/highlights/", StringComparison.Ordinal));
    }

    private static IReadOnlyList<string> Range(int offset, int count) =>
        Enumerable.Range(offset, count).Select(index => $"MLB{index}").ToList();

    private static string SearchPage(IReadOnlyList<string> ids, int total, string domainId = "MLB-NOTEBOOKS") =>
        $$"""{"paging":{"total":{{total}},"offset":0,"limit":20},"results":[{{string.Join(",", ids.Select(id => $$"""{"id":"{{id}}","name":"{{id}}","domain_id":"{{domainId}}"}"""))}}]}""";

    private static (HttpStatusCode Status, string Body)? CatalogProduct(Uri uri, Func<string, (decimal Price, bool Free)?> offerFor)
    {
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2 || segments[0] != "products")
        {
            return null;
        }

        if (segments.Length > 2)
        {
            return (HttpStatusCode.NotFound, """{"message":"No winners found"}""");
        }

        var id = segments[1];
        return offerFor(id) is { } offer
            ? (HttpStatusCode.OK,
                $$"""{"id":"{{id}}","name":"{{id}}","permalink":"https://www.mercadolivre.com.br/p/{{id}}","buy_box_winner":{"item_id":"{{id}}1","price":{{offer.Price.ToString(System.Globalization.CultureInfo.InvariantCulture)}},"shipping":{"free_shipping":{{(offer.Free ? "true" : "false")}}"""
                + "}}}")
            : (HttpStatusCode.OK, $$"""{"id":"{{id}}","name":"{{id}}","buy_box_winner":null}""");
    }

    private static string? QueryValue(Uri uri, string key) =>
        uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .Where(pair => pair[0] == key)
            .Select(pair => Uri.UnescapeDataString(pair.Length > 1 ? pair[1] : string.Empty))
            .FirstOrDefault();

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

        public Func<Uri, (HttpStatusCode Status, string Body)?>? Responder { get; set; }

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

            var (status, content) = Responder?.Invoke(uri) is { } answered
                ? answered
                : PathResponses.TryGetValue(uri.AbsolutePath, out var routed)
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
