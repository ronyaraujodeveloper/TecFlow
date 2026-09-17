using System.Net;
using System.Text;
using System.Text.Json;
using Moq;
using TecFlow.Business.Dto;
using TecFlow.Core.Enums;
using TecFlow.SharedUi.Services.Http;
using TecFlow.SharedUi.Services.Integrations;
using TecFlow.SharedUi.Services.UI;
using TecFlow.Tests.Helpers;

namespace TecFlow.Tests.Unit.SharedUi;

public class MarketplaceOAuthConnectServiceTests
{
    private const string CallbackUri = "https://localhost:7002/integracoes/oauth/callback";
    private const string AuthorizeUrl = "https://partner.test.shopee.com/auth?partner_id=1";

    [Fact]
    public void BuildAuthorizeRelativeUrl_ShouldUsePlatformSlugAndFriendlyName()
    {
        var path = IntegracaoLojaApiService.BuildAuthorizeRelativeUrl(
            MarketplaceType.Shopee,
            CallbackUri,
            "ticket-abc",
            "Loja principal SP",
            "loja-9");

        Assert.Contains("api/marketplace-auth/shopee/authorize-url", path, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("redirectUri=", path, StringComparison.Ordinal);
        Assert.Contains("state=ticket-abc", path, StringComparison.Ordinal);
        Assert.Contains("friendlyName=Loja", path, StringComparison.Ordinal);
        Assert.Contains("lojaId=loja-9", path, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetAuthorizationUrlAsync_ShouldCallPlatformEndpointAndReadAuthorizeUrl()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            captured = request;
            var json = JsonSerializer.Serialize(new MarketplaceAuthorizeUrlResponseDto
            {
                AuthorizeUrl = AuthorizeUrl,
                AuthorizationUrl = AuthorizeUrl,
                Marketplace = "Shopee"
            });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var loading = new Mock<ILoadingService>();
        loading.Setup(service => service.BeginScope(It.IsAny<string?>())).Returns(new NoopDisposable());

        var api = new IntegracaoLojaApiService(
            new NamedClientFactory(handler),
            new StaticTokenProvider("jwt-token"),
            loading.Object);

        var result = await api.GetAuthorizationUrlAsync(
            MarketplaceType.Shopee,
            CallbackUri,
            "ticket-1",
            "Loja SP");

        Assert.True(result.Success);
        Assert.Equal(AuthorizeUrl, result.AuthorizationUrl);
        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Get, captured!.Method);
        Assert.Contains(
            "api/marketplace-auth/shopee/authorize-url",
            captured.RequestUri!.ToString(),
            StringComparison.OrdinalIgnoreCase);
        Assert.Equal("jwt-token", captured.Headers.Authorization?.Parameter);
        loading.Verify(service => service.BeginScope("Gerando URL de autorização..."), Times.Once);
    }

    [Fact]
    public async Task GetAuthorizationUrlAsync_ShouldFailWhenApiOmitsAuthorizeUrl()
    {
        var handler = StubHttpMessageHandler.WithJsonResponse("""{"marketplace":"TikTokShop"}""");
        var loading = new Mock<ILoadingService>();
        loading.Setup(service => service.BeginScope(It.IsAny<string?>())).Returns(new NoopDisposable());

        var api = new IntegracaoLojaApiService(
            new NamedClientFactory(handler),
            new StaticTokenProvider(null),
            loading.Object);

        var result = await api.GetAuthorizationUrlAsync(MarketplaceType.TikTokShop, CallbackUri);

        Assert.False(result.Success);
        Assert.Null(result.AuthorizationUrl);
        Assert.Equal("A API não retornou a URL de autorização.", result.ErrorMessage);
    }

    [Fact]
    public async Task StartAsync_ShouldCreatePendingTicketAndReturnAuthorizeUrl()
    {
        var api = new Mock<IIntegracaoLojaApiService>();
        var pending = new Mock<IIntegracaoLojaPendingLinkStore>();
        pending.Setup(store => store.Create(MarketplaceType.TikTokShop, "Loja TikTok"))
            .Returns("ticket-xyz");
        api.Setup(service => service.GetAuthorizationUrlAsync(
                MarketplaceType.TikTokShop,
                CallbackUri,
                "ticket-xyz",
                "Loja TikTok",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "https://auth.tiktok.test/oauth", null));

        var connect = new MarketplaceOAuthConnectService(api.Object, pending.Object);
        var result = await connect.StartAsync(MarketplaceType.TikTokShop, "Loja TikTok", CallbackUri);

        Assert.True(result.Success);
        Assert.Equal("https://auth.tiktok.test/oauth", result.AuthorizeUrl);
        pending.Verify(store => store.Create(MarketplaceType.TikTokShop, "Loja TikTok"), Times.Once);
    }

    [Fact]
    public async Task StartAsync_ShouldSurfaceApiError_WhenAuthorizeUrlIsMissing()
    {
        var api = new Mock<IIntegracaoLojaApiService>();
        var pending = new Mock<IIntegracaoLojaPendingLinkStore>();
        pending.Setup(store => store.Create(It.IsAny<MarketplaceType>(), It.IsAny<string>()))
            .Returns("ticket");
        api.Setup(service => service.GetAuthorizationUrlAsync(
                It.IsAny<MarketplaceType>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, null, "Não foi possível contactar a API para iniciar OAuth."));

        var connect = new MarketplaceOAuthConnectService(api.Object, pending.Object);
        var result = await connect.StartAsync(MarketplaceType.Shopee, "Loja Shopee", CallbackUri);

        Assert.False(result.Success);
        Assert.Null(result.AuthorizeUrl);
        Assert.Equal("Não foi possível contactar a API para iniciar OAuth.", result.ErrorMessage);
    }

    private sealed class NamedClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public NamedClientFactory(HttpMessageHandler handler)
        {
            _client = new HttpClient(handler, disposeHandler: false)
            {
                BaseAddress = new Uri("https://localhost:7001/")
            };
        }

        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class StaticTokenProvider : IAccessTokenProvider
    {
        private readonly string? _token;

        public StaticTokenProvider(string? token) => _token = token;

        public string? GetAccessToken() => _token;
    }

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
