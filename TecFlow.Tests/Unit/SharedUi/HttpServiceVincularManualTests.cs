using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TecFlow.Business.Dto;
using TecFlow.Core.Enums;
using TecFlow.SharedUi.Services.Http;
using TecFlow.SharedUi.Services.Integrations;
using TecFlow.SharedUi.Services.UI;
using TecFlow.Tests.Helpers;

namespace TecFlow.Tests.Unit.SharedUi;

public class HttpServiceVincularManualTests
{
    private const string ManualLinkPath = "api/marketplace-auth/vincular-manual";

    private const string SuccessEnvelopeJson =
        """{"status":true,"descricao":"Loja vinculada com sucesso.","data":{"id":9,"userId":1,"shopId":"123456","friendlyName":"Loja Homolog","platformType":1,"expiresAt":"2026-10-17T00:00:00Z","status":1,"createdAt":"2026-09-17T00:00:00Z","accessToken":"homolog-test-access-token"}}""";

    [Fact]
    public void IntegracaoLojaDto_ShouldKeepAuthorizationCodeAsStringAndShopIdNumeric()
    {
        const string formJson =
            """{"platformType":1,"authorizationCode":"code_teste","shopId":"123456","friendlyName":"Loja Homolog"}""";

        var dto = JsonSerializer.Deserialize<IntegracaoLojaDto>(formJson, JsonOptions());

        Assert.NotNull(dto);
        Assert.Equal(MarketplaceType.Shopee, dto!.PlatformType);
        Assert.Equal("code_teste", dto.AuthorizationCode);
        Assert.Equal("123456", dto.ShopId);
        Assert.True(long.TryParse(dto.ShopId, out var shopId));
        Assert.Equal(123456L, shopId);
    }

    [Fact]
    public void IntegracaoLojaDto_ShouldDeserializeSwappedFieldsWithoutThrowing()
    {
        const string swappedJson =
            """{"platformType":1,"authorizationCode":"123456","shopId":"code_teste","friendlyName":"Loja Homolog"}""";

        var dto = JsonSerializer.Deserialize<IntegracaoLojaDto>(swappedJson, JsonOptions());

        Assert.NotNull(dto);
        Assert.Equal("123456", dto!.AuthorizationCode);
        Assert.Equal("code_teste", dto.ShopId);
        Assert.False(long.TryParse(dto.ShopId, out _));
    }

    [Fact]
    public async Task HttpService_ShouldPostManualLinkAndDeserializeEnvelope()
    {
        HttpRequestMessage? captured = null;
        string? posted = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            captured = request;
            posted = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonOk(SuccessEnvelopeJson);
        });

        var http = CreateHttp(handler);
        var result = await http.PostAsync<IntegracaoLojaDto, IntegracaoLojaResponseDto>(
            ManualLinkPath,
            CreateValidFormDto());

        Assert.True(result.Success);
        Assert.True(result.Data!.Status);
        Assert.Equal("123456", result.Data.Data!.ShopId);
        Assert.NotNull(captured);
        Assert.Contains(ManualLinkPath, captured!.RequestUri!.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"authorizationCode\":\"code_teste\"", posted, StringComparison.Ordinal);
        Assert.Contains("\"shopId\":\"123456\"", posted, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HttpService_ShouldFailGracefully_WhenStatusIsNumericProblemDetails()
    {
        var handler = StubHttpMessageHandler.WithJsonResponse(
            """{"type":"https://tools.ietf.org/html/rfc9110#section-15.6.1","title":"Internal Server Error","status":500,"detail":"falha simulada"}""",
            HttpStatusCode.InternalServerError);

        var http = CreateHttp(handler);
        var result = await http.PostAsync<IntegracaoLojaDto, IntegracaoLojaResponseDto>(
            ManualLinkPath,
            CreateValidFormDto());

        Assert.False(result.Success);
        Assert.Equal(500, result.StatusCode);
        Assert.Equal("Erro na API (500).", result.ErrorMessage);
    }

    [Fact]
    public async Task HttpService_ShouldFailGracefully_WhenPayloadIsNotJson()
    {
        var handler = StubHttpMessageHandler.WithJsonResponse("<html>gateway</html>", HttpStatusCode.OK);
        var http = CreateHttp(handler);

        var result = await http.PostAsync<IntegracaoLojaDto, IntegracaoLojaResponseDto>(
            ManualLinkPath,
            CreateValidFormDto());

        Assert.False(result.Success);
        Assert.Equal("Não foi possível interpretar a resposta do servidor.", result.ErrorMessage);
    }

    [Fact]
    public async Task IntegracaoLojaApiService_ShouldMapFormPayloadAndReadSuccessEnvelope()
    {
        string? posted = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            posted = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonOk(SuccessEnvelopeJson);
        });

        var api = CreateApi(handler);
        var result = await api.LinkAsync(CreateValidFormDto());

        Assert.True(result.Status);
        Assert.Equal("Loja vinculada com sucesso.", result.Descricao);
        Assert.Contains("code_teste", posted, StringComparison.Ordinal);
        Assert.Contains("123456", posted, StringComparison.Ordinal);
    }

    [Fact]
    public async Task IntegracaoLojaApiService_ShouldReturnEnvelope_WhenJsonCannotBeDeserialized()
    {
        var handler = StubHttpMessageHandler.WithJsonResponse("{not-json", HttpStatusCode.OK);
        var api = CreateApi(handler);

        var result = await api.LinkAsync(CreateValidFormDto());

        Assert.False(result.Status);
        Assert.Equal("Não foi possível interpretar a resposta do servidor.", result.Descricao);
    }

    private static IntegracaoLojaDto CreateValidFormDto() => new()
    {
        PlatformType = MarketplaceType.Shopee,
        AuthorizationCode = "code_teste",
        ShopId = "123456",
        FriendlyName = "Loja Homolog"
    };

    private static HttpService CreateHttp(HttpMessageHandler handler) =>
        new(new NamedClientFactory(handler), new StaticTokenProvider("jwt"), NullLogger<HttpService>.Instance);

    private static IntegracaoLojaApiService CreateApi(HttpMessageHandler handler)
    {
        var loading = new Mock<ILoadingService>();
        loading.Setup(service => service.BeginScope(It.IsAny<string?>())).Returns(new NoopDisposable());
        return new IntegracaoLojaApiService(
            new NamedClientFactory(handler),
            new StaticTokenProvider("jwt"),
            loading.Object,
            NullLogger<IntegracaoLojaApiService>.Instance);
    }

    private static HttpResponseMessage JsonOk(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private static JsonSerializerOptions JsonOptions() => new() { PropertyNameCaseInsensitive = true };

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
