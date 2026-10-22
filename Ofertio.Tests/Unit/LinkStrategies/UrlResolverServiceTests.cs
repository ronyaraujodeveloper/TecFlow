using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Ofertio.Business.Integrations.Common;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Infrastructure.Services.LinkStrategies;
using Ofertio.Tests.Helpers;

namespace Ofertio.Tests.Unit.LinkStrategies;

public class UrlResolverServiceTests
{
    [Fact]
    public async Task ResolveCanonicalAsync_ShouldUseHeadFinalUriAsMarketplace()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Equal(HttpMethod.Head, request.Method);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = new HttpRequestMessage(
                    request.Method,
                    "https://shopee.com.br/product-i.111.222")
            };
        });

        var service = new UrlResolverService(
            new NamedClientFactory(handler),
            new StaticExpansion("https://pelando.com.br/d/abc"),
            NullLogger<UrlResolverService>.Instance);

        var result = await service.ResolveCanonicalAsync("https://pelando.com.br/d/abc");

        Assert.True(result.IsMarketplace);
        Assert.Contains("shopee.com.br", result.CanonicalUrl, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResolveCanonicalAsync_ShouldConvertMeliSocialRedirectIntoFocusedProduct()
    {
        var handler = new StubHttpMessageHandler(request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            RequestMessage = new HttpRequestMessage(
                request.Method,
                "https://www.mercadolivre.com.br/social/mastertechjr?matt_tool=88888888&matt_d2id=x#item_id=MLB4012345678")
        });

        var service = new UrlResolverService(
            new NamedClientFactory(handler),
            new StaticExpansion("https://meli.la/1aBcDeF"),
            NullLogger<UrlResolverService>.Instance);

        var result = await service.ResolveCanonicalAsync("https://meli.la/1aBcDeF");

        Assert.Equal("https://produto.mercadolivre.com.br/MLB-4012345678", result.CanonicalUrl);
    }

    private sealed class NamedClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public NamedClientFactory(HttpMessageHandler handler)
        {
            _client = new HttpClient(handler, disposeHandler: false)
            {
                BaseAddress = new Uri("https://localhost/")
            };
        }

        public HttpClient CreateClient(string name)
        {
            Assert.Equal(IntegrationHttpClientNames.UrlResolver, name);
            return _client;
        }
    }

    private sealed class StaticExpansion : IUrlExpansionService
    {
        private readonly string _url;

        public StaticExpansion(string url) => _url = url;

        public Task<string> ExpandUrlAsync(string shortenedUrl, CancellationToken cancellationToken = default) =>
            Task.FromResult(_url);
    }
}
