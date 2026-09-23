using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TecFlow.Business.Integrations.MercadoLivre;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Core.Enums;
using TecFlow.Database.Entity;

namespace TecFlow.Tests.Unit.LinkStrategies;

public class MercadoLivreLinkStrategyTests
{
    private static readonly Guid TestTenantId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private const string TrackingId = "987654321";
    private const string FriendlyName = "Loja ML Homolog";
    private const string ItemId = "MLB1234567890";

    [Theory]
    [InlineData("https://www.mercadolivre.com.br/p/MLB1234567890", true)]
    [InlineData("https://produto.mercadolivre.com.br/MLB-1234567890-cadeira", true)]
    [InlineData("https://mercadolivre.com/sec/abc123", true)]
    [InlineData("https://ml.com.br/MLB-99999999", true)]
    [InlineData("https://shopee.com.br/produto-i.1.2", false)]
    [InlineData("https://example.com/p/MLB123", false)]
    public void CanProcess_ShouldRecognizeMercadoLivreHosts(string url, bool expected)
    {
        Assert.Equal(expected, CreateStrategy().CanProcess(url));
    }

    [Theory]
    [InlineData("https://www.mercadolivre.com.br/p/MLB1234567890", "MLB1234567890")]
    [InlineData("https://produto.mercadolivre.com.br/MLB-1234567890-produto-titulo", "MLB1234567890")]
    [InlineData("https://www.mercadolivre.com.br/item_JM?p=MLB-55555555", "MLB55555555")]
    public void Parser_ShouldExtractMlbItemId(string url, string expected)
    {
        Assert.True(MercadoLivreProductUrlParser.TryParse(url, out var itemId));
        Assert.Equal(expected, itemId);
    }

    [Fact]
    public async Task GenerateDeepLinkAsync_ShouldInjectMattToolAndMattWord()
    {
        var strategy = CreateStrategy();

        var link = await strategy.GenerateDeepLinkAsync(
            "https://produto.mercadolivre.com.br/MLB-1234567890-cadeira",
            Guid.NewGuid(),
            "aff-10");

        Assert.Contains($"/p/{ItemId}", link, StringComparison.OrdinalIgnoreCase);
        AssertMattParams(link);
    }

    [Fact]
    public async Task GenerateDeepLinkAsync_ShouldKeepSecShortUrlAndExistingQuery()
    {
        const string shortUrl = "https://mercadolivre.com/sec/abc123?utm_source=share";
        var strategy = CreateStrategy();

        var link = await strategy.GenerateDeepLinkAsync(shortUrl, Guid.NewGuid(), "aff-10");

        Assert.Contains("/sec/abc123", link, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("utm_source=share", link, StringComparison.OrdinalIgnoreCase);
        AssertMattParams(link);
    }

    [Fact]
    public void PlatformLinkResolver_ShouldSelectMercadoLivreStrategy()
    {
        var resolver = new PlatformLinkResolver(
            [CreateStrategy()],
            NullLogger<PlatformLinkResolver>.Instance);

        var selected = resolver.Resolve("https://www.mercadolivre.com.br/p/MLB1");
        Assert.Equal(MarketplaceType.MercadoLivre, selected.PlatformType);
        Assert.Equal("Mercado Livre", selected.PlatformName);
    }

    private static void AssertMattParams(string link)
    {
        var decoded = Uri.UnescapeDataString(link.Replace("+", " ", StringComparison.Ordinal));
        Assert.Contains($"{MercadoLivreCommissionUrlBuilder.MattToolQuery}={TrackingId}", decoded, StringComparison.Ordinal);
        Assert.Contains($"{MercadoLivreCommissionUrlBuilder.MattWordQuery}={FriendlyName}", decoded, StringComparison.Ordinal);
    }

    private static MercadoLivreLinkStrategy CreateStrategy(IntegracaoLoja? store = null) =>
        new(
            new PassthroughUrlExpansionService(),
            new FixedStoreResolver(store ?? CreateStore()),
            new AffiliateLinkGenerationContext { UserId = 10 },
            CreateHostEnvironment(),
            NullLogger<MercadoLivreLinkStrategy>.Instance);

    private static IHostEnvironment CreateHostEnvironment()
    {
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(item => item.EnvironmentName).Returns("Homologacao");
        return environment.Object;
    }

    private static IntegracaoLoja CreateStore() =>
        new()
        {
            Id = 3,
            UserId = 10,
            TenantId = TestTenantId,
            ShopId = "ul-ml-10-loja-ml-homolog",
            FriendlyName = FriendlyName,
            AffiliateTrackingId = TrackingId,
            AccessToken = "token",
            PlatformType = MarketplaceType.MercadoLivre
        };

    private sealed class PassthroughUrlExpansionService : IUrlExpansionService
    {
        public Task<string> ExpandUrlAsync(string shortenedUrl, CancellationToken cancellationToken = default) =>
            Task.FromResult(shortenedUrl);
    }

    private sealed class FixedStoreResolver : IIntegracaoLojaScopeResolver
    {
        private readonly IntegracaoLoja _store;

        public FixedStoreResolver(IntegracaoLoja store) => _store = store;

        public Task<IntegracaoLoja> ResolveAsync(
            Guid storeScopeId,
            int userId,
            MarketplaceType expectedPlatform,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_store);
    }
}
