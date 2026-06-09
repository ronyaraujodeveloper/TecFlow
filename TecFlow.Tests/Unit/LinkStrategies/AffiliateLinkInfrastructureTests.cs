using System.Net;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Application;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Core.Enums;
using TecFlow.Infrastructure.Services.LinkStrategies;
using TecFlow.Tests.Helpers;

namespace TecFlow.Tests.Unit.LinkStrategies;

public class UrlExpansionServiceTests
{
    [Fact]
    public async Task ExpandUrlAsync_ShouldFollowLocationHeaderUntilFinalUrl()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri!.Host.Contains("s.shopee.com.br"))
            {
                return new HttpResponseMessage(HttpStatusCode.Found)
                {
                    Headers = { Location = new Uri("https://shopee.com.br/produto-i.123.456") }
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var factory = new StubHttpClientFactory(handler);
        var service = new UrlExpansionService(factory, Microsoft.Extensions.Logging.Abstractions.NullLogger<UrlExpansionService>.Instance);

        var expanded = await service.ExpandUrlAsync("https://s.shopee.com.br/abc");

        Assert.Equal("https://shopee.com.br/produto-i.123.456", expanded);
    }
}

public class AffiliateLinkGenerationServiceTests
{
    [Fact]
    public async Task GenerateAsync_ShouldReturnFriendlyFailure_WhenPlatformIsUnsupported()
    {
        var strategies = new IPlatformLinkStrategy[]
        {
            new ShopeeLinkStrategy(
                new NoOpUrlExpansionService(),
                new NoOpStoreResolver(),
                new NoOpShopeeClient(),
                new AffiliateLinkGenerationContext(),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<ShopeeLinkStrategy>.Instance)
        };

        var service = new AffiliateLinkGenerationService(
            new PlatformLinkResolver(strategies, Microsoft.Extensions.Logging.Abstractions.NullLogger<PlatformLinkResolver>.Instance),
            new NoOpUrlExpansionService(),
            new NoOpStoreResolver(),
            new NoOpShortLinkService(),
            new AffiliateLinkGenerationContext(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<AffiliateLinkGenerationService>.Instance);

        var result = await service.GenerateAsync(
            new GerarLinkAfiliadoDto
            {
                OriginalUrl = "https://example.com/produto",
                StoreId = IntegracaoLojaScopeHelper.EncodeStoreScope(1)
            },
            userId: 10);

        Assert.False(result.Success);
        Assert.Contains("suportada", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class NoOpUrlExpansionService : IUrlExpansionService
    {
        public Task<string> ExpandUrlAsync(string shortenedUrl, CancellationToken cancellationToken = default) =>
            Task.FromResult(shortenedUrl);
    }

    private sealed class NoOpStoreResolver : IIntegracaoLojaScopeResolver
    {
        public Task<TecFlow.Database.Entity.IntegracaoLoja> ResolveAsync(
            Guid storeScopeId,
            int userId,
            MarketplaceType expectedPlatform,
            CancellationToken cancellationToken = default) =>
            throw new AffiliateLinkGenerationException("Loja mock indisponível.");
    }

    private sealed class NoOpShopeeClient : IShopeeAffiliateLinkClient
    {
        public Task<string> GenerateCustomLinkAsync(
            TecFlow.Database.Entity.IntegracaoLoja store,
            string expandedProductUrl,
            string affiliateId,
            string? customNickname,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(expandedProductUrl);
    }

    private sealed class NoOpShortLinkService : IShortLinkService
    {
        public Task<(string PublicShortUrl, Guid AffiliateLinkId)> CreateShortLinkAsync(
            string destinationUrl,
            string originalUrl,
            MarketplaceType platformType,
            int userId,
            Guid tenantId,
            int? integracaoLojaId,
            string? customNickname,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(("http://localhost:5001/r/mock123", Guid.NewGuid()));
    }
}

internal sealed class StubHttpClientFactory : IHttpClientFactory
{
    private readonly HttpMessageHandler _handler;

    public StubHttpClientFactory(HttpMessageHandler handler) => _handler = handler;

    public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
}
