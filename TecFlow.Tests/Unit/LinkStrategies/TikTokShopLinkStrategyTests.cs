using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TecFlow.Business.Integrations.TikTokShop;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Core.Enums;
using TecFlow.Database.Entity;

namespace TecFlow.Tests.Unit.LinkStrategies;

public class TikTokShopLinkStrategyTests
{
    private static readonly Guid TestTenantId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private const string TrackingId = "18325850271";
    private const string ProductId = "1729382256910270123";
    private const string CanonicalUrl = "https://shop.tiktok.com/view/product/1729382256910270123";

    [Theory]
    [InlineData("https://tiktok.com/view/product/123", true)]
    [InlineData("https://www.tiktok.com/shop/pdp/abc123", true)]
    [InlineData("https://shop.tiktok.com/view/product/999", true)]
    [InlineData("https://vt.tiktok.com/ZSabcde/", true)]
    [InlineData("https://vm.tiktok.com/abc", true)]
    [InlineData("https://example.com/produto", false)]
    [InlineData("https://shopee.com.br/produto-i.1.2", false)]
    public void CanProcess_ShouldRecognizeTikTokShopHosts(string url, bool expected)
    {
        var strategy = CreateStrategy();
        Assert.Equal(expected, strategy.CanProcess(url));
    }

    [Theory]
    [InlineData("https://shop.tiktok.com/view/product/1729382256910270123", "1729382256910270123")]
    [InlineData("https://www.tiktok.com/shop/pdp/8899", "8899")]
    [InlineData("https://tiktok.com/product/555?utm_source=share", "555")]
    [InlineData("https://shop.tiktok.com/item?product_id=777", "777")]
    [InlineData("https://shop.tiktok.com/item?itemId=888", "888")]
    public void Parser_ShouldExtractProductId(string url, string expected)
    {
        Assert.True(TikTokShopProductUrlParser.TryParse(url, out var productId));
        Assert.Equal(expected, productId);
    }

    [Fact]
    public async Task GenerateDeepLinkAsync_ShouldInjectTrackingIdAsSubId()
    {
        var strategy = CreateStrategy(store: CreateStore(trackingId: TrackingId, friendlyName: "Loja TikTok"));

        var link = await strategy.GenerateDeepLinkAsync(CanonicalUrl, Guid.NewGuid(), "aff-10");

        Assert.Equal($"https://shop.tiktok.com/view/product/{ProductId}?sub_id={TrackingId}", link);
    }

    [Fact]
    public async Task GenerateDeepLinkAsync_ShouldFallbackToFriendlyNameWhenTrackingIdIsMissing()
    {
        var strategy = CreateStrategy(store: CreateStore(trackingId: null, friendlyName: "Achadinhos de Aaz"));

        var link = await strategy.GenerateDeepLinkAsync(CanonicalUrl, Guid.NewGuid(), "aff-10");

        Assert.Equal(
            $"https://shop.tiktok.com/view/product/{ProductId}?sub_id={Uri.EscapeDataString("Achadinhos de Aaz")}",
            link);
    }

    [Fact]
    public async Task GenerateDeepLinkAsync_ShouldExpandShortUrlThenInjectSubId()
    {
        var expansion = new StubUrlExpansionService(CanonicalUrl);
        var strategy = CreateStrategy(expansion, CreateStore(TrackingId, "Loja TikTok"));

        var link = await strategy.GenerateDeepLinkAsync("https://vt.tiktok.com/ZSabcde/", Guid.NewGuid(), "aff-10");

        Assert.Equal($"https://shop.tiktok.com/view/product/{ProductId}?sub_id={TrackingId}", link);
        Assert.Equal("https://vt.tiktok.com/ZSabcde/", expansion.LastRequestedUrl);
    }

    [Fact]
    public void PlatformLinkResolver_ShouldSelectTikTokShopStrategy()
    {
        var resolver = new PlatformLinkResolver(
            [CreateStrategy()],
            NullLogger<PlatformLinkResolver>.Instance);

        var selected = resolver.Resolve("https://shop.tiktok.com/view/product/1");
        Assert.Equal(MarketplaceType.TikTokShop, selected.PlatformType);
        Assert.Equal("TikTok Shop", selected.PlatformName);
    }

    private static TikTokShopLinkStrategy CreateStrategy(
        IUrlExpansionService? expansion = null,
        IntegracaoLoja? store = null) =>
        new(
            expansion ?? new PassthroughUrlExpansionService(),
            new FixedStoreResolver(store ?? CreateStore(TrackingId, "Loja TikTok")),
            new AffiliateLinkGenerationContext { UserId = 10 },
            CreateHostEnvironment(),
            NullLogger<TikTokShopLinkStrategy>.Instance);

    private static IHostEnvironment CreateHostEnvironment()
    {
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(item => item.EnvironmentName).Returns("Homologacao");
        return environment.Object;
    }

    private static IntegracaoLoja CreateStore(string? trackingId, string friendlyName) =>
        new()
        {
            Id = 2,
            UserId = 10,
            TenantId = TestTenantId,
            ShopId = "ul-tt-10-loja-tiktok",
            FriendlyName = friendlyName,
            AffiliateTrackingId = trackingId,
            AccessToken = "token",
            PlatformType = MarketplaceType.TikTokShop
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
