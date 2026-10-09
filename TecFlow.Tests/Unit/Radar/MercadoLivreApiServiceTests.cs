using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging.Abstractions;
using TecFlow.Infrastructure.Services.Radar;

namespace TecFlow.Tests.Unit.Radar;

public class MercadoLivreApiServiceTests
{
    [Fact]
    public async Task SearchProductsAsync_ShouldCallPublicMlbSearchWithoutAuthorization()
    {
        var handler = new CaptureHandler();
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.mercadolibre.com/")
        };
        var sut = new MercadoLivreApiService(http, NullLogger<MercadoLivreApiService>.Instance);

        var result = await sut.SearchProductsAsync("dell i7", 20);

        Assert.NotNull(handler.LastRequest);
        Assert.Null(handler.LastRequest!.Headers.Authorization);
        Assert.Equal(
            "https://api.mercadolibre.com/sites/MLB/search?q=dell%20i7&limit=20",
            handler.LastRequest.RequestUri!.AbsoluteUri);
        var item = Assert.Single(result.Items);
        Assert.Equal("Dell i7", item.ProductName);
        Assert.Equal(10m, item.Price);
        Assert.Equal("https://http2.mlstatic.com/t.jpg", item.ImageUrl);
        Assert.Equal("https://produto.mercadolivre.com.br/MLB-1", item.SourceUrl);
        Assert.False(result.MissingCredentials);
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            const string json =
                """{"results":[{"id":"MLB1","title":"Dell i7","price":10,"permalink":"https://produto.mercadolivre.com.br/MLB-1","thumbnail":"https://http2.mlstatic.com/t.jpg"}]}""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });
        }
    }
}
