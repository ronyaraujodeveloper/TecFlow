using System.Net;
using System.Net.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TecFlow.Business.Integrations.MercadoLivre;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database;
using TecFlow.Infrastructure.Services.Radar;
using TecFlow.Util.Security;

namespace TecFlow.Tests.Unit.Radar;

public class MercadoLivreApiServiceTests
{
    [Fact]
    public async Task SearchProductsAsync_ShouldAskForConnectedAccountWhenTokenIsMissing()
    {
        var handler = new CaptureHandler();
        var sut = CreateSut(handler, new MercadoLivreIntegrationOptions());

        var result = await sut.SearchProductsAsync(1, "dell i7", 20);

        Assert.True(result.MissingCredentials);
        Assert.Null(handler.LastRequest);
    }

    [Fact]
    public async Task SearchProductsAsync_ShouldSendBearerTokenFromAppSettings()
    {
        var handler = new CaptureHandler();
        var sut = CreateSut(handler, new MercadoLivreIntegrationOptions { AccessToken = "ml-app-token" });

        var result = await sut.SearchProductsAsync(1, "dell i7", 20);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal("Bearer", handler.LastRequest!.Headers.Authorization?.Scheme);
        Assert.Equal("ml-app-token", handler.LastRequest.Headers.Authorization?.Parameter);
        Assert.Equal(
            "https://api.mercadolibre.com/sites/MLB/search?q=dell%20i7&limit=20",
            handler.LastRequest.RequestUri!.AbsoluteUri);
        Assert.Equal("Dell i7", Assert.Single(result.Items).ProductName);
    }

    [Fact]
    public async Task SearchProductsAsync_ShouldTreatForbiddenAsMissingAccount()
    {
        var handler = new CaptureHandler { StatusCode = HttpStatusCode.Forbidden };
        var sut = CreateSut(handler, new MercadoLivreIntegrationOptions { AccessToken = "expired" });

        var result = await sut.SearchProductsAsync(1, "dell i7");

        Assert.True(result.MissingCredentials);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task SearchProductsAsync_ShouldSearchWhenAffiliateIdIsRegisteredWithoutOAuth()
    {
        var handler = new CaptureHandler();
        var sut = CreateSut(
            handler,
            new MercadoLivreIntegrationOptions(),
            db =>
            {
                db.MarketplaceAccounts.Add(new MarketplaceAccount
                {
                    TenantId = Guid.NewGuid(),
                    UserId = "9",
                    MarketplaceType = MarketplaceType.Amazon,
                    FriendlyName = "Mercado Livre",
                    TrackingId = "14343296",
                    IsActive = true
                });
            });

        var result = await sut.SearchProductsAsync(1, "dell i7", 20);

        Assert.False(result.MissingCredentials);
        Assert.Null(handler.LastRequest!.Headers.Authorization);
        var item = Assert.Single(result.Items);
        Assert.Contains("matt_tool=14343296", item.SourceUrl, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SearchProductsAsync_ShouldFallbackToPublicListWhenApiReturnsForbidden()
    {
        var handler = new CaptureHandler
        {
            StatusCode = HttpStatusCode.Forbidden,
            HtmlBody = """
                <a title="Notebook Dell i7" href="https://www.mercadolivre.com.br/notebook-dell-i7/p/MLB1234567890">Dell</a>
                """
        };
        var sut = CreateSut(
            handler,
            new MercadoLivreIntegrationOptions(),
            db =>
            {
                db.MarketplaceAccounts.Add(new MarketplaceAccount
                {
                    TenantId = Guid.NewGuid(),
                    UserId = "1",
                    MarketplaceType = MarketplaceType.MercadoLivre,
                    TrackingId = "14343296",
                    IsActive = true
                });
            });

        var result = await sut.SearchProductsAsync(1, "dell i7", 20, "14343296");

        Assert.False(result.MissingCredentials);
        Assert.Null(result.ErrorMessage);
        var item = Assert.Single(result.Items);
        Assert.Equal("Web", item.Source);
        Assert.Contains("matt_tool=14343296", item.SourceUrl, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("lista.mercadolivre.com.br", handler.LastRequest!.RequestUri!.AbsoluteUri, StringComparison.OrdinalIgnoreCase);
    }

    private static MercadoLivreApiService CreateSut(
        HttpMessageHandler handler,
        MercadoLivreIntegrationOptions options,
        Action<AppDbContext>? seed = null)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.mercadolibre.com/") };
        var dbName = Guid.NewGuid().ToString();
        if (seed is not null)
        {
            using var seedContext = CreateDbContext(dbName);
            seed(seedContext);
            seedContext.SaveChanges();
        }

        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        factory.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CreateDbContext(dbName));
        return new MercadoLivreApiService(
            http,
            Options.Create(options),
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

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

        public string Body { get; set; } =
            """{"results":[{"id":"MLB1","title":"Dell i7","price":10,"permalink":"https://produto.mercadolivre.com.br/MLB-1","thumbnail":"https://http2.mlstatic.com/t.jpg"}]}""";

        public string HtmlBody { get; set; } = "<html></html>";

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            var isLista = request.RequestUri?.Host.Contains("lista.mercadolivre", StringComparison.OrdinalIgnoreCase) == true;
            var status = isLista ? HttpStatusCode.OK : StatusCode;
            var body = isLista ? HtmlBody : Body;
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body)
            });
        }
    }
}
