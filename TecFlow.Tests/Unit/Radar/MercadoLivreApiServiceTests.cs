using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TecFlow.Infrastructure.Services.Radar;

namespace TecFlow.Tests.Unit.Radar;

public class MercadoLivreApiServiceTests
{
    [Fact]
    public async Task SearchProductsAsync_ShouldCallPublicMlbSearchWithRequiredUserAgent()
    {
        var handler = new CaptureHandler();
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.mercadolibre.com/")
        };
        var sut = new MercadoLivreApiService(http, NullLogger<MercadoLivreApiService>.Instance);

        var result = await sut.SearchProductsAsync("dell i7", 20);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(
            "https://api.mercadolibre.com/sites/MLB/search?q=dell%20i7&limit=20",
            handler.LastRequest!.RequestUri!.AbsoluteUri);
        Assert.Contains(
            MercadoLivreApiService.PublicUserAgent,
            handler.LastRequest.Headers.UserAgent.ToString()
                + string.Join(' ', handler.LastRequest.Headers.GetValues("User-Agent")));
        var item = Assert.Single(result.Items);
        Assert.Equal("Dell i7", item.ProductName);
        Assert.Equal(10m, item.Price);
        Assert.Equal("https://http2.mlstatic.com/t.jpg", item.ImageUrl);
        Assert.Equal("https://produto.mercadolivre.com.br/MLB-1", item.SourceUrl);
    }

    [Fact]
    public async Task SearchProductsAsync_ShouldLogWarningWhenMlbReturnsError()
    {
        var handler = new CaptureHandler { StatusCode = HttpStatusCode.Forbidden };
        using var http = new HttpClient(handler);
        var logger = new Mock<ILogger<MercadoLivreApiService>>();
        var sut = new MercadoLivreApiService(http, logger.Object);

        var result = await sut.SearchProductsAsync("dell i7");

        Assert.Empty(result.Items);
        logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Erro na API ML")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            const string json =
                """{"results":[{"id":"MLB1","title":"Dell i7","price":10,"permalink":"https://produto.mercadolivre.com.br/MLB-1","thumbnail":"https://http2.mlstatic.com/t.jpg"}]}""";
            return Task.FromResult(new HttpResponseMessage(StatusCode)
            {
                Content = new StringContent(json)
            });
        }
    }
}
