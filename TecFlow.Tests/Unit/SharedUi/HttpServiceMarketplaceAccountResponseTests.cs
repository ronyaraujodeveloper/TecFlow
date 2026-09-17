using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using TecFlow.Business.Dto;
using TecFlow.SharedUi.Services.Http;
using TecFlow.Tests.Helpers;

namespace TecFlow.Tests.Unit.SharedUi;

public class HttpServiceMarketplaceAccountResponseTests
{
    [Fact]
    public async Task HttpService_ShouldDeserializeMarketplaceAccountEnvelope_WithCamelCaseAndNumericShopId()
    {
        const string json =
            """{"status":true,"descricao":"Loja vinculada com sucesso","data":{"id":9,"userId":1,"shopId":123456,"friendlyName":"Loja Homolog","shopName":"Loja Homolog","platformType":1,"expiresAt":"2026-10-17T00:00:00Z","status":1,"createdAt":"2026-09-17T00:00:00Z"}}""";

        var http = CreateHttp(json);
        var result = await http.PostAsync<object, MarketplaceAccountResponseDto>(
            "api/marketplace-auth/vincular-manual",
            new { });

        Assert.True(result.Success);
        Assert.True(result.Data!.Status);
        Assert.Equal("123456", result.Data.Data!.ShopId);
        Assert.Equal("Loja Homolog", result.Data.Data.FriendlyName);
    }

    [Fact]
    public async Task HttpService_ShouldDeserializePascalCaseAndStringEnums()
    {
        const string json =
            """{"Status":true,"Descricao":"Loja vinculada com sucesso","Data":{"Id":3,"UserId":7,"ShopId":"123456","FriendlyName":"Loja Homolog","PlatformType":"Shopee","ExpiresAt":"2026-10-17T00:00:00Z","Status":"Active","CreatedAt":"2026-09-17T00:00:00Z"}}""";

        var http = CreateHttp(json);
        var result = await http.PostAsync<object, MarketplaceAccountResponseDto>(
            "api/marketplace-auth/vincular-manual",
            new { });

        Assert.True(result.Success);
        Assert.Equal(TecFlow.Core.Enums.MarketplaceType.Shopee, result.Data!.Data!.PlatformType);
        Assert.Equal(TecFlow.Core.Enums.MarketplaceIntegrationStatus.Active, result.Data.Data.Status);
    }

    [Fact]
    public async Task HttpService_ShouldLogAndFail_WhenJsonIsInvalid()
    {
        var http = CreateHttp("<html>erro</html>");
        var result = await http.PostAsync<object, MarketplaceAccountResponseDto>(
            "api/marketplace-auth/vincular-manual",
            new { });

        Assert.False(result.Success);
        Assert.Equal("Não foi possível interpretar a resposta do servidor.", result.ErrorMessage);
    }

    private static HttpService CreateHttp(string json)
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        return new HttpService(
            new NamedClientFactory(handler),
            new StaticTokenProvider("jwt"),
            NullLogger<HttpService>.Instance);
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
