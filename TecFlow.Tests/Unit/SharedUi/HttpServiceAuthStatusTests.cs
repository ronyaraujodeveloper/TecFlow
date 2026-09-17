using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using TecFlow.Business.Dto;
using TecFlow.SharedUi.Services.Http;
using TecFlow.Tests.Helpers;

namespace TecFlow.Tests.Unit.SharedUi;

public class HttpServiceAuthStatusTests
{
    [Fact]
    public async Task GetAsync_ShouldSurface401MessageFromApiEnvelope()
    {
        var handler = StubHttpMessageHandler.WithJsonResponse(
            """{"message":"Usuário não autenticado.","errorCode":"UNAUTHORIZED"}""",
            HttpStatusCode.Unauthorized);

        var http = new HttpService(
            new NamedClientFactory(handler),
            new StaticTokenProvider(null),
            NullLogger<HttpService>.Instance);

        var result = await http.GetAsync<IntegracaoLojaResponseDto>("api/integracoes/lojas");

        Assert.False(result.Success);
        Assert.Equal(401, result.StatusCode);
        Assert.Equal("Usuário não autenticado.", result.ErrorMessage);
        Assert.Equal("UNAUTHORIZED", result.ErrorCode);
    }

    [Fact]
    public async Task PostAsync_ShouldSurface500FallbackMessage()
    {
        var handler = StubHttpMessageHandler.WithJsonResponse(
            """{"title":"Internal Server Error","status":500}""",
            HttpStatusCode.InternalServerError);

        var http = new HttpService(
            new NamedClientFactory(handler),
            new StaticTokenProvider("jwt"),
            NullLogger<HttpService>.Instance);

        var result = await http.PostAsync<GerarLinkAfiliadoDto, GerarLinkAfiliadoResponseDto>(
            "api/afiliados/links/gerar",
            new GerarLinkAfiliadoDto { OriginalUrl = "https://shopee.com.br/x" });

        Assert.False(result.Success);
        Assert.Equal(500, result.StatusCode);
        Assert.Equal("Erro na API (500).", result.ErrorMessage);
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
}
