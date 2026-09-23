using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TecFlow.Business.Integrations.MagazineLuiza;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Core.Enums;
using TecFlow.Database.Entity;

namespace TecFlow.Tests.Unit.LinkStrategies;

public class MagazineLuizaLinkStrategyTests
{
    private static readonly Guid TestTenantId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private const string PartnerSlug = "magazinematos";
    private const string PartnerNumeric = "987654";
    private const string ProductId = "218434100";
    private const string CanonicalUrl = "https://www.magazineluiza.com.br/geladeira/p/218434100/ed/refg/";

    [Theory]
    [InlineData("https://www.magazineluiza.com.br/p/218434100/", true)]
    [InlineData("https://www.magazinevoce.com.br/magazinematos/p/218434100/", true)]
    [InlineData("https://magalu.me/abc123", true)]
    [InlineData("https://mglz.ne/xyz", true)]
    [InlineData("https://shopee.com.br/produto-i.1.2", false)]
    [InlineData("https://example.com/p/218434100", false)]
    public void CanProcess_ShouldRecognizeMagaluHosts(string url, bool expected)
    {
        Assert.Equal(expected, CreateStrategy().CanProcess(url));
    }

    [Theory]
    [InlineData("https://www.magazineluiza.com.br/geladeira/p/218434100/ed/refg/", "218434100")]
    [InlineData("https://www.magazinevoce.com.br/magazinematos/geladeira/p/218434100/", "218434100")]
    [InlineData("https://www.magazineluiza.com.br/p/ABC12345", "ABC12345")]
    [InlineData("https://www.magazineluiza.com.br/busca?productId=218434100", "218434100")]
    public void Parser_ShouldExtractProductId(string url, string expected)
    {
        Assert.True(MagazineLuizaProductUrlParser.TryParse(url, out var productId));
        Assert.Equal(expected, productId);
    }

    [Fact]
    public async Task GenerateDeepLinkAsync_ShouldBuildMagazineVoceUrl()
    {
        var strategy = CreateStrategy(store: CreateStore(PartnerSlug));

        var link = await strategy.GenerateDeepLinkAsync(CanonicalUrl, Guid.NewGuid(), "aff-10");

        Assert.Equal($"https://www.magazinevoce.com.br/{PartnerSlug}/p/{ProductId}/", link);
    }

    [Fact]
    public async Task GenerateDeepLinkAsync_ShouldBuildParceiroQueryWhenPartnerIsNumeric()
    {
        var strategy = CreateStrategy(store: CreateStore(PartnerNumeric));

        var link = await strategy.GenerateDeepLinkAsync(CanonicalUrl, Guid.NewGuid(), "aff-10");

        Assert.Equal($"https://www.magazineluiza.com.br/p/{ProductId}/?parceiro={PartnerNumeric}", link);
    }

    [Fact]
    public async Task GenerateDeepLinkAsync_ShouldExpandMagaluMeThenBuildAffiliateUrl()
    {
        var expansion = new StubUrlExpansionService(CanonicalUrl);
        var strategy = CreateStrategy(expansion, CreateStore(PartnerSlug));

        var link = await strategy.GenerateDeepLinkAsync("https://magalu.me/abc123", Guid.NewGuid(), "aff-10");

        Assert.Equal($"https://www.magazinevoce.com.br/{PartnerSlug}/p/{ProductId}/", link);
        Assert.Equal("https://magalu.me/abc123", expansion.LastRequestedUrl);
    }

    [Fact]
    public void PlatformLinkResolver_ShouldSelectMagazineLuizaStrategy()
    {
        var resolver = new PlatformLinkResolver(
            [CreateStrategy()],
            NullLogger<PlatformLinkResolver>.Instance);

        var selected = resolver.Resolve("https://magalu.me/abc");
        Assert.Equal(MarketplaceType.MagazineLuiza, selected.PlatformType);
        Assert.Equal("Magazine Luiza", selected.PlatformName);
    }

    private static MagazineLuizaLinkStrategy CreateStrategy(
        IUrlExpansionService? expansion = null,
        IntegracaoLoja? store = null) =>
        new(
            expansion ?? new PassthroughUrlExpansionService(),
            new FixedStoreResolver(store ?? CreateStore(PartnerSlug)),
            new AffiliateLinkGenerationContext { UserId = 10 },
            CreateHostEnvironment(),
            NullLogger<MagazineLuizaLinkStrategy>.Instance);

    private static IHostEnvironment CreateHostEnvironment()
    {
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(item => item.EnvironmentName).Returns("Homologacao");
        return environment.Object;
    }

    private static IntegracaoLoja CreateStore(string trackingId) =>
        new()
        {
            Id = 5,
            UserId = 10,
            TenantId = TestTenantId,
            ShopId = "ul-mg-10-loja-magalu",
            FriendlyName = "Loja Magalu Homolog",
            AffiliateTrackingId = trackingId,
            AccessToken = "token",
            PlatformType = MarketplaceType.MagazineLuiza
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
