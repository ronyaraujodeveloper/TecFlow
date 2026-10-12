using System.Net;
using System.Net.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
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
    [Fact]
    public async Task SearchProductsAsync_ShouldAskForOAuthWhenTokenIsMissing()
    {
        var handler = new CaptureHandler();
        var sut = CreateSut(handler, new MercadoLivreIntegrationOptions());

        var result = await sut.SearchProductsAsync(1, "dell i7", 20);

        Assert.True(result.MissingCredentials);
        Assert.Null(handler.LastRequest);
    }

    [Fact]
    public async Task SearchProductsAsync_ShouldNotTreatAffiliateIdAsSearchCredential()
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
                    UserId = "1",
                    MarketplaceType = MarketplaceType.MercadoLivre,
                    TrackingId = "14343296",
                    IsActive = true
                });
            });

        var result = await sut.SearchProductsAsync(1, "dell i7", 20, "14343296");

        Assert.True(result.MissingCredentials);
        Assert.Empty(result.Items);
        Assert.Null(handler.LastRequest);
    }

    [Fact]
    public async Task SearchProductsAsync_ShouldSendBearerTokenAndAccessTokenQuery()
    {
        var handler = new CaptureHandler();
        var sut = CreateSut(handler, new MercadoLivreIntegrationOptions { AccessToken = "ml-app-token" });

        var result = await sut.SearchProductsAsync(1, "dell i7", 20);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal("Bearer", handler.LastRequest!.Headers.Authorization?.Scheme);
        Assert.Equal("ml-app-token", handler.LastRequest.Headers.Authorization?.Parameter);
        Assert.Equal(
            "https://api.mercadolibre.com/sites/MLB/search?q=dell%20i7&limit=20&access_token=ml-app-token",
            handler.LastRequest.RequestUri!.AbsoluteUri);
        Assert.Equal("Dell i7", Assert.Single(result.Items).ProductName);
    }

    [Fact]
    public async Task SearchProductsAsync_ShouldInjectMattToolWhenOAuthSearchSucceeds()
    {
        var handler = new CaptureHandler();
        var sut = CreateSut(
            handler,
            new MercadoLivreIntegrationOptions { AccessToken = "ml-app-token" },
            db =>
            {
                db.MarketplaceAccounts.Add(new MarketplaceAccount
                {
                    TenantId = Guid.NewGuid(),
                    UserId = "1",
                    MarketplaceType = MarketplaceType.MercadoLivre,
                    TrackingId = "14343296",
                    AccessToken = "ml-app-token",
                    ExpiresAt = DateTime.UtcNow.AddHours(4),
                    IsActive = true
                });
            });

        var result = await sut.SearchProductsAsync(1, "dell i7", 20, "14343296");

        Assert.False(result.MissingCredentials);
        var item = Assert.Single(result.Items);
        Assert.Contains("matt_tool=14343296", item.SourceUrl, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Api", item.Source);
    }

    [Fact]
    public async Task SearchProductsAsync_ShouldExposeForbiddenWithoutHtmlFallback()
    {
        var handler = new CaptureHandler { StatusCode = HttpStatusCode.Forbidden };
        var sut = CreateSut(handler, new MercadoLivreIntegrationOptions { AccessToken = "expired" });

        var result = await sut.SearchProductsAsync(1, "dell i7");

        Assert.False(result.MissingCredentials);
        Assert.Empty(result.Items);
        Assert.Equal(OfficialCatalogSearchRules.MercadoLivreSearchForbiddenMessage, result.ErrorMessage);
        Assert.Contains("api.mercadolibre.com", handler.LastRequest!.RequestUri!.Host, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("lista.mercadolivre", handler.LastRequest.RequestUri!.AbsoluteUri, StringComparison.OrdinalIgnoreCase);
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

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(StatusCode)
            {
                Content = new StringContent(Body)
            });
        }
    }
}
