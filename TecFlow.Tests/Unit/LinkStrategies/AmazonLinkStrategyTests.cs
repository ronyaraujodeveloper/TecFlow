using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TecFlow.Business.Integrations.Amazon;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Core.Enums;
using TecFlow.Database.Entity;

namespace TecFlow.Tests.Unit.LinkStrategies;

public class AmazonLinkStrategyTests
{
    private static readonly Guid TestTenantId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private const string AssociateTag = "sualoja-20";
    private const string Asin = "B08N5WRWNW";
    private const string CanonicalUrl = "https://www.amazon.com.br/dp/B08N5WRWNW";

    [Theory]
    [InlineData("https://www.amazon.com.br/dp/B08N5WRWNW", true)]
    [InlineData("https://amazon.com/dp/B08N5WRWNW", true)]
    [InlineData("https://amzn.to/abc123", true)]
    [InlineData("https://a.co/d/xyz", true)]
    [InlineData("https://shopee.com.br/produto-i.1.2", false)]
    [InlineData("https://example.com/dp/B08N5WRWNW", false)]
    public void CanProcess_ShouldRecognizeAmazonHosts(string url, bool expected)
    {
        Assert.Equal(expected, CreateStrategy().CanProcess(url));
    }

    [Theory]
    [InlineData("https://www.amazon.com.br/dp/B08N5WRWNW", "B08N5WRWNW")]
    [InlineData("https://www.amazon.com.br/gp/product/B08N5WRWNW?psc=1", "B08N5WRWNW")]
    [InlineData("https://www.amazon.com/dp/B0TESTASIN/ref=sr_1_1", "B0TESTASIN")]
    [InlineData("https://www.amazon.com.br/produto/dp/1234567890", "1234567890")]
    [InlineData("https://www.amazon.com.br/exec/obidos/ASIN/B08N5WRWNW", "B08N5WRWNW")]
    public void Parser_ShouldExtractAsin(string url, string expected)
    {
        Assert.True(AmazonProductUrlParser.TryParse(url, out var asin));
        Assert.Equal(expected, asin);
    }

    [Fact]
    public async Task GenerateDeepLinkAsync_ShouldInjectAssociateTag()
    {
        var strategy = CreateStrategy();

        var link = await strategy.GenerateDeepLinkAsync(CanonicalUrl, Guid.NewGuid(), "aff-10");

        Assert.Contains($"/dp/{Asin}", link, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"{AmazonCommissionUrlBuilder.TagQuery}={AssociateTag}", link, StringComparison.Ordinal);
        Assert.StartsWith("https://www.amazon.com.br/dp/", link, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateDeepLinkAsync_ShouldExpandShortUrlThenInjectTag()
    {
        var expansion = new StubUrlExpansionService(CanonicalUrl);
        var strategy = CreateStrategy(expansion, CreateStore());

        var link = await strategy.GenerateDeepLinkAsync("https://amzn.to/abc123", Guid.NewGuid(), "aff-10");

        Assert.Contains($"{AmazonCommissionUrlBuilder.TagQuery}={AssociateTag}", link, StringComparison.Ordinal);
        Assert.Contains($"/dp/{Asin}", link, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("https://amzn.to/abc123", expansion.LastRequestedUrl);
    }

    [Fact]
    public async Task GenerateDeepLinkAsync_ShouldExpandAcoShortUrlThenInjectTag()
    {
        var expansion = new StubUrlExpansionService($"https://www.amazon.com.br/gp/product/{Asin}");
        var strategy = CreateStrategy(expansion, CreateStore());

        var link = await strategy.GenerateDeepLinkAsync("https://a.co/d/xyz", Guid.NewGuid(), "aff-10");

        Assert.Equal($"https://www.amazon.com.br/dp/{Asin}?tag={AssociateTag}", link);
        Assert.Equal("https://a.co/d/xyz", expansion.LastRequestedUrl);
    }

    [Fact]
    public void PlatformLinkResolver_ShouldSelectAmazonStrategy()
    {
        var resolver = new PlatformLinkResolver(
            [CreateStrategy()],
            NullLogger<PlatformLinkResolver>.Instance);

        var selected = resolver.Resolve("https://amzn.to/abc");
        Assert.Equal(MarketplaceType.Amazon, selected.PlatformType);
        Assert.Equal("Amazon", selected.PlatformName);
    }

    private static AmazonLinkStrategy CreateStrategy(
        IUrlExpansionService? expansion = null,
        IntegracaoLoja? store = null) =>
        new(
            expansion ?? new PassthroughUrlExpansionService(),
            new FixedStoreResolver(store ?? CreateStore()),
            new AffiliateLinkGenerationContext { UserId = 10 },
            CreateHostEnvironment(),
            NullLogger<AmazonLinkStrategy>.Instance);

    private static IHostEnvironment CreateHostEnvironment()
    {
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(item => item.EnvironmentName).Returns("Homologacao");
        return environment.Object;
    }

    private static IntegracaoLoja CreateStore() =>
        new()
        {
            Id = 4,
            UserId = 10,
            TenantId = TestTenantId,
            ShopId = "ul-am-10-loja-amazon",
            FriendlyName = "Loja Amazon Homolog",
            AffiliateTrackingId = AssociateTag,
            AccessToken = "token",
            PlatformType = MarketplaceType.Amazon
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
