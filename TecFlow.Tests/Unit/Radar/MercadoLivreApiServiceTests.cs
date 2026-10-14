using System.Net;
using System.Net.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TecFlow.Business.Integrations.Auth;
using TecFlow.Business.Integrations.MercadoLivre;
using TecFlow.Business.Service.Radar;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database;
using TecFlow.Infrastructure.Services.Radar;
using TecFlow.Util.Security;

namespace TecFlow.Tests.Unit.Radar;

public class MercadoLivreApiServiceTests
{
    private const string ListaHtml =
        """
        <ol>
          <li class="ui-search-layout__item">
            <a class="ui-search-link" href="https://produto.mercadolivre.com.br/MLB-123-notebook-dell-i7">
              <h2>Notebook Dell i7</h2>
            </a>
            <span class="andes-money-amount__fraction">4.299</span>
            <img src="https://http2.mlstatic.com/n.jpg" />
          </li>
        </ol>
        """;

    [Fact]
    public async Task SearchProductsAsync_ShouldAskForOAuthWhenTokenIsMissingAndListaIsBlocked()
    {
        var handler = new RoutingHandler { ListaBody = "<html>account-verification</html>" };
        var sut = CreateSut(handler, new MercadoLivreIntegrationOptions());

        var result = await sut.SearchProductsAsync(1, "dell i7", 20);

        Assert.True(result.MissingCredentials);
        Assert.DoesNotContain(handler.Requests, request => request.Host == "api.mercadolibre.com");
    }

    [Fact]
    public async Task SearchProductsAsync_ShouldNotTreatAffiliateIdAsSearchCredential()
    {
        var handler = new RoutingHandler { ListaBody = "<html>account-verification</html>" };
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
        var handler = new RoutingHandler { ListaBody = "<html>account-verification</html>" };
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
        var search = Assert.Single(handler.Requests, request => request.Path.Contains("sites/MLB/search", StringComparison.Ordinal));
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
        Assert.Equal(2, handler.Requests.Count(request => request.Path.Contains("sites/MLB/search", StringComparison.Ordinal)));
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
        var search = Assert.Single(handler.Requests, request => request.Path.Contains("sites/MLB/search", StringComparison.Ordinal));
        Assert.Equal("Bearer APP_USR-new", search.Authorization);
        await using var verify = CreateDbContext(dbName);
        var stored = await verify.MarketplaceAccounts.IgnoreQueryFilters().SingleAsync();
        Assert.Equal("APP_USR-new", stored.AccessToken);
        Assert.Equal("TG-new", stored.RefreshToken);
        Assert.True(stored.ExpiresAt > DateTime.UtcNow.AddHours(5));
    }

    [Fact]
    public async Task SearchProductsAsync_ShouldSendBearerTokenAndAccessTokenQuery()
    {
        var handler = new RoutingHandler();
        var sut = CreateSut(handler, new MercadoLivreIntegrationOptions { AccessToken = "ml-app-token" });

        var result = await sut.SearchProductsAsync(1, "dell i7", 20);

        var search = Assert.Single(handler.Requests);
        Assert.Equal("Bearer ml-app-token", search.Authorization);
        Assert.Equal(
            "https://api.mercadolibre.com/sites/MLB/search?q=dell%20i7&limit=20&access_token=ml-app-token",
            search.Url);
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
    public async Task SearchProductsAsync_OnForbidden_ShouldFallbackToListaWithMattTool()
    {
        var handler = new RoutingHandler { SearchStatus = HttpStatusCode.Forbidden, SearchBody = """{"message":"forbidden"}""" };
        var sut = CreateSut(
            handler,
            new MercadoLivreIntegrationOptions { AccessToken = "app-token" },
            db => db.MarketplaceAccounts.Add(MercadoLivreAccount(trackingId: "14343296")));

        var result = await sut.SearchProductsAsync(1, "dell i7");

        Assert.Null(result.ErrorMessage);
        var item = Assert.Single(result.Items);
        Assert.Equal("Web", item.Source);
        Assert.Equal("Notebook Dell i7", item.ProductName);
        Assert.Contains("matt_tool=14343296", item.SourceUrl, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(handler.Requests, request => request.Url == "https://lista.mercadolivre.com.br/dell-i7");
    }

    [Fact]
    public async Task SearchProductsAsync_OnUserScopeError_ShouldFallbackToLista()
    {
        var handler = new RoutingHandler
        {
            SearchStatus = HttpStatusCode.BadRequest,
            SearchBody = """{"message":"missing required scopes","error":"unauthorized_scopes"}"""
        };
        var sut = CreateSut(handler, new MercadoLivreIntegrationOptions { AccessToken = "app-token" });

        var result = await sut.SearchProductsAsync(1, "dell i7");

        Assert.Equal("Web", Assert.Single(result.Items).Source);
    }

    [Fact]
    public async Task SearchProductsAsync_OnForbiddenAndBlockedLista_ShouldExposeForbiddenMessage()
    {
        var handler = new RoutingHandler
        {
            SearchStatus = HttpStatusCode.Forbidden,
            ListaBody = "<html>account-verification</html>"
        };
        var sut = CreateSut(handler, new MercadoLivreIntegrationOptions { AccessToken = "expired" });

        var result = await sut.SearchProductsAsync(1, "dell i7");

        Assert.False(result.MissingCredentials);
        Assert.Empty(result.Items);
        Assert.Equal(OfficialCatalogSearchRules.MercadoLivreSearchForbiddenMessage, result.ErrorMessage);
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
        return new AppDbContext(dbOptions, encryption.Object, new TecFlow.Database.MultiTenancy.NullCurrentTenantService());
    }

    private sealed record CapturedRequest(string Url, string Host, string Path, string? Authorization, string Body);

    private sealed class RoutingHandler : HttpMessageHandler
    {
        public List<CapturedRequest> Requests { get; } = [];

        public HttpStatusCode SearchStatus { get; set; } = HttpStatusCode.OK;

        public string SearchBody { get; set; } =
            """{"results":[{"id":"MLB1","title":"Dell i7","price":10,"permalink":"https://produto.mercadolivre.com.br/MLB-1","thumbnail":"https://http2.mlstatic.com/t.jpg"}]}""";

        public string TokenBody { get; set; } = """{"error":"invalid_client"}""";

        public string ListaBody { get; set; } = ListaHtml;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var uri = request.RequestUri!;
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new CapturedRequest(
                uri.AbsoluteUri,
                uri.Host,
                uri.AbsolutePath,
                request.Headers.Authorization?.ToString(),
                body));

            var (status, content) = uri.AbsolutePath.Contains("oauth/token", StringComparison.Ordinal)
                ? (TokenBody.Contains("access_token", StringComparison.Ordinal) ? HttpStatusCode.OK : HttpStatusCode.BadRequest, TokenBody)
                : uri.Host.StartsWith("lista.", StringComparison.OrdinalIgnoreCase)
                    ? (HttpStatusCode.OK, ListaBody)
                    : (SearchStatus, SearchBody);
            return new HttpResponseMessage(status)
            {
                RequestMessage = request,
                Content = new StringContent(content)
            };
        }
    }
}
