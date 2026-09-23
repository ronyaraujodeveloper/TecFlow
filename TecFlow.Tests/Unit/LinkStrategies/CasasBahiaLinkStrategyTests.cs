using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TecFlow.Business.Integrations.CasasBahia;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Core.Enums;
using TecFlow.Database.Entity;

namespace TecFlow.Tests.Unit.LinkStrategies;

public class CasasBahiaLinkStrategyTests
{
    private static readonly Guid TestTenantId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private const string ParceiroId = "tecflow_cb";
    private const string FriendlyName = "Loja CB Homolog";
    private const string ProductId = "55014612";
    private const string CanonicalUrl = "https://www.casasbahia.com.br/p/55014612";

    [Theory]
    [InlineData("https://www.casasbahia.com.br/p/55014612", true)]
    [InlineData("https://casasbahia.com.br/cadeira/55014612/p", true)]
    [InlineData("https://cb.com.br/abc123", true)]
    [InlineData("https://casasbahia.app.link/xyz", true)]
    [InlineData("https://shopee.com.br/produto-i.1.2", false)]
    [InlineData("https://example.com/p/55014612", false)]
    public void CanProcess_ShouldRecognizeCasasBahiaHosts(string url, bool expected)
    {
        Assert.Equal(expected, CreateStrategy().CanProcess(url));
    }

    [Theory]
    [InlineData("https://www.casasbahia.com.br/p/55014612", "55014612")]
    [InlineData("https://www.casasbahia.com.br/cadeira-gamer/p/55014612/", "55014612")]
    [InlineData("https://www.casasbahia.com.br/55014612/p", "55014612")]
    [InlineData("https://www.casasbahia.com.br/busca?productId=55014612", "55014612")]
    public void Parser_ShouldExtractProductId(string url, string expected)
    {
        Assert.True(CasasBahiaProductUrlParser.TryParse(url, out var productId));
        Assert.Equal(expected, productId);
    }

    [Fact]
    public async Task GenerateDeepLinkAsync_ShouldInjectParceiroAndSubId()
    {
        var strategy = CreateStrategy();

        var link = await strategy.GenerateDeepLinkAsync(CanonicalUrl, Guid.NewGuid(), "aff-10");

        Assert.Equal(
            $"https://www.casasbahia.com.br/p/{ProductId}?parceiro={ParceiroId}&sub_id={Uri.EscapeDataString(FriendlyName)}",
            link);
    }

    [Fact]
    public async Task GenerateDeepLinkAsync_ShouldExpandCbShortUrlThenInjectParams()
    {
        var expansion = new StubUrlExpansionService(CanonicalUrl);
        var strategy = CreateStrategy(expansion, CreateStore());

        var link = await strategy.GenerateDeepLinkAsync("https://cb.com.br/abc123", Guid.NewGuid(), "aff-10");

        Assert.Contains($"parceiro={ParceiroId}", link, StringComparison.Ordinal);
        Assert.Contains($"sub_id={Uri.EscapeDataString(FriendlyName)}", link, StringComparison.Ordinal);
        Assert.Contains($"/p/{ProductId}", link, StringComparison.Ordinal);
        Assert.Equal("https://cb.com.br/abc123", expansion.LastRequestedUrl);
    }

    [Fact]
    public async Task GenerateDeepLinkAsync_ShouldExpandAppLinkThenInjectParams()
    {
        var expansion = new StubUrlExpansionService($"https://www.casasbahia.com.br/{ProductId}/p");
        var strategy = CreateStrategy(expansion, CreateStore());

        var link = await strategy.GenerateDeepLinkAsync(
            "https://casasbahia.app.link/xyz",
            Guid.NewGuid(),
            "aff-10");

        Assert.Equal(
            $"https://www.casasbahia.com.br/p/{ProductId}?parceiro={ParceiroId}&sub_id={Uri.EscapeDataString(FriendlyName)}",
            link);
        Assert.Equal("https://casasbahia.app.link/xyz", expansion.LastRequestedUrl);
    }

    [Fact]
    public void PlatformLinkResolver_ShouldSelectCasasBahiaStrategy()
    {
        var resolver = new PlatformLinkResolver(
            [CreateStrategy()],
            NullLogger<PlatformLinkResolver>.Instance);

        var selected = resolver.Resolve("https://cb.com.br/abc");
        Assert.Equal(MarketplaceType.CasasBahia, selected.PlatformType);
        Assert.Equal("Casas Bahia", selected.PlatformName);
    }

    private static CasasBahiaLinkStrategy CreateStrategy(
        IUrlExpansionService? expansion = null,
        IntegracaoLoja? store = null) =>
        new(
            expansion ?? new PassthroughUrlExpansionService(),
            new FixedStoreResolver(store ?? CreateStore()),
            new AffiliateLinkGenerationContext { UserId = 10 },
            CreateHostEnvironment(),
            NullLogger<CasasBahiaLinkStrategy>.Instance);

    private static IHostEnvironment CreateHostEnvironment()
    {
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(item => item.EnvironmentName).Returns("Homologacao");
        return environment.Object;
    }

    private static IntegracaoLoja CreateStore() =>
        new()
        {
            Id = 7,
            UserId = 10,
            TenantId = TestTenantId,
            ShopId = "ul-cb-10-loja-cb",
            FriendlyName = FriendlyName,
            AffiliateTrackingId = ParceiroId,
            AccessToken = "token",
            PlatformType = MarketplaceType.CasasBahia
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
