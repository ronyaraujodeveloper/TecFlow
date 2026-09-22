using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TecFlow.Business.Dto;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.SharedUi.Models;
using TecFlow.SharedUi.Services.Http;
using TecFlow.SharedUi.Services.LinkGenerator;
using TecFlow.SharedUi.Services.UI;
using TecFlow.Tests.Helpers;

namespace TecFlow.Tests.Unit.SharedUi;

public class GeradorLinksServiceTests
{
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private const string ProductUrl = "https://shopee.com.br/produto-i.123.456";
    private const string ShortUrl = "http://localhost:5001/r/abc1234";

    [Fact]
    public async Task AffiliateLinkApiService_ShouldPostToPortugueseGenerateEndpointAndReturnConvertedLink()
    {
        GerarLinkAfiliadoDto? captured = null;
        string? capturedPath = null;
        var http = new Mock<IHttpService>();
        http.Setup(service => service.PostAsync<GerarLinkAfiliadoDto, GerarLinkAfiliadoResponseDto>(
                It.IsAny<string>(),
                It.IsAny<GerarLinkAfiliadoDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string path, GerarLinkAfiliadoDto body, CancellationToken _) =>
            {
                capturedPath = path;
                captured = body;
                return ApiResult<GerarLinkAfiliadoResponseDto>.Ok(new GerarLinkAfiliadoResponseDto
                {
                    Success = true,
                    Message = "Link de afiliado gerado com sucesso.",
                    OriginalUrl = ProductUrl,
                    AffiliateUrl = "https://shopee.com.br/produto-i.123.456?tracking_code=tecflow_sandbox_subid",
                    ConvertedUrl = "https://shopee.com.br/produto-i.123.456?tracking_code=tecflow_sandbox_subid",
                    ShortenedUrl = ShortUrl,
                    PlatformDetected = "Shopee",
                    AffiliateLinkId = Guid.Parse("11111111-2222-3333-4444-555555555555")
                });
            });

        var loading = new Mock<ILoadingService>();
        loading.Setup(service => service.BeginScope(It.IsAny<string?>())).Returns(new NoopDisposable());

        var api = new AffiliateLinkApiService(http.Object, loading.Object);
        var request = new GerarLinkAfiliadoDto
        {
            OriginalUrl = ProductUrl,
            StoreId = IntegracaoLojaScopeHelper.EncodeStoreScope(7),
            TenantId = TenantId,
            ShopId = "shop-sandbox"
        };

        var result = await api.GenerateAsync(request);

        Assert.Equal(AffiliateLinkApiService.GeneratePath, capturedPath);
        Assert.Equal("api/links/convert", capturedPath);
        Assert.NotNull(captured);
        Assert.Equal(ProductUrl, captured!.OriginalUrl);
        Assert.Equal("shop-sandbox", captured.ShopId);
        Assert.Equal(TenantId, captured.TenantId);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.True(result.Data!.Success);
        Assert.Equal(ProductUrl, result.Data.OriginalUrl);
        Assert.Equal("https://shopee.com.br/produto-i.123.456?tracking_code=tecflow_sandbox_subid", result.Data.AffiliateUrl);
        Assert.Equal(ShortUrl, result.Data.ShortenedUrl);
        Assert.Equal("Shopee", result.Data.PlatformDetected);
        loading.Verify(service => service.BeginScope("Gerando link de comissão..."), Times.Once);
    }

    [Fact]
    public async Task HttpService_ShouldDeserializeGenerateSuccessFromAfiliadosLinksEndpoint()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            captured = request;
            var json = JsonSerializer.Serialize(new GerarLinkAfiliadoResponseDto
            {
                Success = true,
                Message = "Link de afiliado gerado com sucesso.",
                OriginalUrl = ProductUrl,
                AffiliateUrl = "https://shopee.com.br/produto-i.123.456?tracking_code=tecflow_sandbox_subid",
                ConvertedUrl = "https://shopee.com.br/produto-i.123.456?tracking_code=tecflow_sandbox_subid",
                ShortenedUrl = ShortUrl,
                PlatformDetected = "Shopee",
                AffiliateLinkId = Guid.NewGuid()
            });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var http = new HttpService(
            new NamedClientFactory(handler),
            new StaticTokenProvider("token-teste"),
            NullLogger<HttpService>.Instance);

        var result = await http.PostAsync<GerarLinkAfiliadoDto, GerarLinkAfiliadoResponseDto>(
            AffiliateLinkApiService.GeneratePath,
            new GerarLinkAfiliadoDto
            {
                OriginalUrl = ProductUrl,
                StoreId = IntegracaoLojaScopeHelper.EncodeStoreScope(1),
                TenantId = TenantId,
                ShopId = "shop-1"
            });

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Post, captured!.Method);
        Assert.Contains("api/links/convert", captured.RequestUri!.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.True(result.Success);
        Assert.Equal(ProductUrl, result.Data!.OriginalUrl);
        Assert.Equal("https://shopee.com.br/produto-i.123.456?tracking_code=tecflow_sandbox_subid", result.Data.AffiliateUrl);
        Assert.Equal(ShortUrl, result.Data.ShortenedUrl);
        Assert.Equal("Shopee", result.Data.PlatformDetected);
        Assert.True(result.Data.Success);
    }

    [Fact]
    public async Task HttpService_ShouldCapture200OkFromLinksConvert_WithConvertedUrlAlias()
    {
        const string json =
            """{"status":true,"descricao":"OK","convertedUrl":"http://localhost:5001/r/taeej22s","platformDetected":"Shopee"}""";

        var handler = StubHttpMessageHandler.WithJsonResponse(json, HttpStatusCode.OK);
        var http = new HttpService(
            new NamedClientFactory(handler),
            new StaticTokenProvider("token-teste"),
            NullLogger<HttpService>.Instance);

        var result = await http.PostAsync<GerarLinkAfiliadoDto, GerarLinkAfiliadoResponseDto>(
            AffiliateLinkApiService.GeneratePath,
            new GerarLinkAfiliadoDto { OriginalUrl = "https://br.shp.ee/taeej22s" });

        Assert.True(result.Success);
        Assert.True(result.Data!.HasConvertedLink);
        Assert.Equal("http://localhost:5001/r/taeej22s", result.Data.ShortenedUrl);
        Assert.Equal("http://localhost:5001/r/taeej22s", result.Data.ConvertedUrl);
        Assert.Equal("Shopee", result.Data.PlatformDetected);
    }

    [Fact]
    public async Task HttpService_ShouldCaptureDualAffiliateAndShortenedUrlsFromLinksConvert()
    {
        const string json =
            """{"status":true,"descricao":"OK","originalUrl":"https://br.shp.ee/taeej22s","affiliateUrl":"https://shopee.com.br/universal-link/product/999999/888888?utm_source=affiliate&sub_id=tecflow_test","convertedUrl":"https://shopee.com.br/universal-link/product/999999/888888?utm_source=affiliate&sub_id=tecflow_test","shortenedUrl":"http://localhost:5001/r/taeej22s","platformDetected":"Shopee"}""";

        var handler = StubHttpMessageHandler.WithJsonResponse(json, HttpStatusCode.OK);
        var http = new HttpService(
            new NamedClientFactory(handler),
            new StaticTokenProvider("token-teste"),
            NullLogger<HttpService>.Instance);

        var result = await http.PostAsync<GerarLinkAfiliadoDto, GerarLinkAfiliadoResponseDto>(
            AffiliateLinkApiService.GeneratePath,
            new GerarLinkAfiliadoDto { OriginalUrl = "https://br.shp.ee/taeej22s" });

        Assert.True(result.Success);
        Assert.True(result.Data!.HasConvertedLink);
        Assert.Equal("https://br.shp.ee/taeej22s", result.Data.OriginalUrl);
        Assert.Equal(
            "https://shopee.com.br/universal-link/product/999999/888888?utm_source=affiliate&sub_id=tecflow_test",
            result.Data.AffiliateUrl);
        Assert.Equal(
            "https://shopee.com.br/universal-link/product/999999/888888?utm_source=affiliate&sub_id=tecflow_test",
            result.Data.ConvertedUrl);
        Assert.Equal("http://localhost:5001/r/taeej22s", result.Data.ShortenedUrl);
    }

    [Fact]
    public async Task HttpService_ShouldSurfaceApiFailureMessageForInvalidUrl()
    {
        var handler = StubHttpMessageHandler.WithJsonResponse(
            """{"success":false,"message":"Informe a URL do produto para gerar o link de afiliado.","shortenedUrl":"","platformDetected":""}""",
            HttpStatusCode.BadRequest);

        var http = new HttpService(
            new NamedClientFactory(handler),
            new StaticTokenProvider(null),
            NullLogger<HttpService>.Instance);

        var result = await http.PostAsync<GerarLinkAfiliadoDto, GerarLinkAfiliadoResponseDto>(
            AffiliateLinkApiService.GeneratePath,
            new GerarLinkAfiliadoDto { OriginalUrl = "https://example.com/x" });

        Assert.False(result.Success);
        Assert.Equal("Informe a URL do produto para gerar o link de afiliado.", result.ErrorMessage);
        Assert.Equal(400, result.StatusCode);
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
