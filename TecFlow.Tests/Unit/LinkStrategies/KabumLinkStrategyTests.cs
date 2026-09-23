using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TecFlow.Business.Integrations.Kabum;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Core.Enums;
using TecFlow.Database.Entity;

namespace TecFlow.Tests.Unit.LinkStrategies;

public class KabumLinkStrategyTests
{
    private static readonly Guid TestTenantId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private const string TrackingId = "tecflow_kabum";
    private const string ProductId = "123456";
    private const string CanonicalUrl = "https://www.kabum.com.br/produto/123456/placa-de-video";

    [Theory]
    [InlineData("https://www.kabum.com.br/produto/123456", true)]
    [InlineData("https://kabum.com.br/produto/9999/item", true)]
    [InlineData("https://kb.um/abc123", true)]
    [InlineData("https://kabum.me/xyz", true)]
    [InlineData("https://shopee.com.br/produto-i.1.2", false)]
    [InlineData("https://example.com/produto/123456", false)]
    public void CanProcess_ShouldRecognizeKabumHosts(string url, bool expected)
    {
        Assert.Equal(expected, CreateStrategy().CanProcess(url));
    }

    [Theory]
    [InlineData("https://www.kabum.com.br/produto/123456/placa-de-video", "123456")]
    [InlineData("https://www.kabum.com.br/produto/98765432", "98765432")]
    [InlineData("https://www.kabum.com.br/busca?productId=123456", "123456")]
    public void Parser_ShouldExtractProductId(string url, string expected)
    {
        Assert.True(KabumProductUrlParser.TryParse(url, out var productId));
        Assert.Equal(expected, productId);
    }

    [Fact]
    public async Task GenerateDeepLinkAsync_ShouldInjectSubIdAndUtmSource()
    {
        var strategy = CreateStrategy();

        var link = await strategy.GenerateDeepLinkAsync(CanonicalUrl, Guid.NewGuid(), "aff-10");

        Assert.Equal(
            $"https://www.kabum.com.br/produto/{ProductId}?sub_id={TrackingId}&utm_source=afiliado",
            link);
    }

    [Fact]
    public async Task GenerateDeepLinkAsync_ShouldFallbackToFriendlyNameWhenTrackingIdIsMissing()
    {
        var strategy = CreateStrategy(store: CreateStore(trackingId: null, friendlyName: "Loja Kabum"));

        var link = await strategy.GenerateDeepLinkAsync(CanonicalUrl, Guid.NewGuid(), "aff-10");

        Assert.Contains("sub_id=Loja%20Kabum", link, StringComparison.Ordinal);
        Assert.Contains("utm_source=afiliado", link, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GenerateDeepLinkAsync_ShouldExpandKbUmThenInjectParams()
    {
        var expansion = new StubUrlExpansionService(CanonicalUrl);
        var strategy = CreateStrategy(expansion, CreateStore(TrackingId, "Loja Kabum"));

        var link = await strategy.GenerateDeepLinkAsync("https://kb.um/abc123", Guid.NewGuid(), "aff-10");

        Assert.Equal(
            $"https://www.kabum.com.br/produto/{ProductId}?sub_id={TrackingId}&utm_source=afiliado",
            link);
        Assert.Equal("https://kb.um/abc123", expansion.LastRequestedUrl);
    }

    [Fact]
    public void PlatformLinkResolver_ShouldSelectKabumStrategy()
    {
        var resolver = new PlatformLinkResolver(
            [CreateStrategy()],
            NullLogger<PlatformLinkResolver>.Instance);

        var selected = resolver.Resolve("https://kb.um/abc");
        Assert.Equal(MarketplaceType.Kabum, selected.PlatformType);
        Assert.Equal("Kabum!", selected.PlatformName);
    }

    private static KabumLinkStrategy CreateStrategy(
        IUrlExpansionService? expansion = null,
        IntegracaoLoja? store = null) =>
        new(
            expansion ?? new PassthroughUrlExpansionService(),
            new FixedStoreResolver(store ?? CreateStore(TrackingId, "Loja Kabum Homolog")),
            new AffiliateLinkGenerationContext { UserId = 10 },
            CreateHostEnvironment(),
            NullLogger<KabumLinkStrategy>.Instance);

    private static IHostEnvironment CreateHostEnvironment()
    {
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(item => item.EnvironmentName).Returns("Homologacao");
        return environment.Object;
    }

    private static IntegracaoLoja CreateStore(string? trackingId, string friendlyName) =>
        new()
        {
            Id = 6,
            UserId = 10,
            TenantId = TestTenantId,
            ShopId = "ul-kb-10-loja-kabum",
            FriendlyName = friendlyName,
            AffiliateTrackingId = trackingId,
            AccessToken = "token",
            PlatformType = MarketplaceType.Kabum
        };

    private sealed class PassthroughUrlExpansionService : IUrlExpansionService
    {
        public Task<string> ExpandUrlAsync(string shortenedUrl, CancellationToken cancellationToken = default) =>
            Task.FromResult(shortenedUrl);
    }

    private sealed class StubUrlExpansionService : IUrlExpansionService
    {
        private readonly string _expanded;

        public StubUrlExpansionService(string expanded) => _expanded = expanded;

        public string? LastRequestedUrl { get; private set; }

        public Task<string> ExpandUrlAsync(string shortenedUrl, CancellationToken cancellationToken = default)
        {
            LastRequestedUrl = shortenedUrl;
            return Task.FromResult(_expanded);
        }
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
