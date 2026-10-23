using Microsoft.Extensions.Logging.Abstractions;
using Ofertio.Business.Integrations.MercadoLivre;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.LinkStrategies;
using Ofertio.Core.Enums;
using Ofertio.Database.Entity;

namespace Ofertio.Tests.Unit.LinkStrategies;

public class MercadoLivreLinkStrategyTests
{
    private static readonly Guid TestTenantId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private const string TrackingId = "987654321";
    private const string FriendlyName = "Loja ML Homolog";
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

        Assert.StartsWith("https://produto.mercadolivre.com.br/MLB-1234567890-cadeira?", link, StringComparison.Ordinal);
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

    [Theory]
    [InlineData("https://www.mercadolivre.com.br/p/MLB20621890", "MLB20621890")]
    [InlineData("https://www.mercadolivre.com.br/p/MLB20621890?matt_tool=97303724&matt_word=97303724", "MLB20621890")]
    [InlineData("https://produto.mercadolivre.com.br/MLB-2062189012-item", "MLB2062189012")]
    [InlineData("https://produto.mercadolivre.com.br/MLB2062189012", "MLB2062189012")]
    [InlineData("https://produto.mercadolivre.com.br/MLB-3456789012_JM#position=1", "MLB3456789012")]
    public void Parser_ShouldExtractCatalogAndListingIdsWithoutChangingDigits(string url, string expected)
    {
        Assert.True(MercadoLivreProductUrlParser.TryParse(url, out var itemId));
        Assert.Equal(expected, itemId);
    }

    [Theory]
    [InlineData("https://www.mercadolivre.com.br/social/matosadalzira?matt_word=lumiaraujo&matt_tool=14343296&forceInApp=true")]
    [InlineData("https://www.mercadolivre.com.br/kit-smartphone/up/MLBU5500574101")]
    [InlineData("https://www.mercadolivre.com.br/ofertas?id=MLB1234567890123456")]
    public void Parser_ShouldNotInventAnIdForLinksWithoutProductCode(string url)
    {
        Assert.False(MercadoLivreProductUrlParser.TryParse(url, out _));
        Assert.Throws<AffiliateLinkGenerationException>(() => MercadoLivreProductUrlParser.ParseOrThrow(url));
    }

    [Theory]
    [InlineData("https://www.mercadolivre.com.br/p/MLB20621890?x=1", true)]
    [InlineData("https://produto.mercadolivre.com.br/MLB-2062189012-item", true)]
    [InlineData("https://www.mercadolivre.com.br/item_JM?p=MLB-55555555", false)]
    [InlineData("https://www.mercadolivre.com.br/social/loja", false)]
    public void HasProductIdInPath_ShouldDetectIdsOnlyInThePath(string url, bool expected)
    {
        Assert.Equal(expected, MercadoLivreProductUrlParser.HasProductIdInPath(url));
    }

    [Fact]
    public async Task GenerateDeepLinkAsync_CatalogUrl_ShouldKeepProductIdAndReplaceOnlyMattParams()
    {
        var link = await CreateStrategy().GenerateDeepLinkAsync(
            "https://www.mercadolivre.com.br/p/MLB20621890?matt_tool=97303724&matt_word=97303724",
            Guid.NewGuid(),
            "aff-10");

        Assert.StartsWith("https://www.mercadolivre.com.br/p/MLB20621890?", link, StringComparison.Ordinal);
        Assert.DoesNotContain("produto.mercadolivre.com.br", link, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MLB1234567890", link, StringComparison.Ordinal);
        Assert.DoesNotContain("97303724", link, StringComparison.Ordinal);
        AssertMattParams(link);
    }

    [Fact]
    public async Task GenerateDeepLinkAsync_CatalogUrlWithSlug_ShouldKeepTheProductPage()
    {
        var link = await CreateStrategy().GenerateDeepLinkAsync(
            "https://www.mercadolivre.com.br/oculos-havaianas-conchas-preto/p/MLB20621890#reco_item_pos=1&reco_backend=item_decorator",
            Guid.NewGuid(),
            "aff-10");

        Assert.StartsWith(
            "https://www.mercadolivre.com.br/oculos-havaianas-conchas-preto/p/MLB20621890?",
            link,
            StringComparison.Ordinal);
        Assert.DoesNotContain("produto.mercadolivre.com.br", link, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("#", link, StringComparison.Ordinal);
        AssertMattParams(link);
    }

    [Fact]
    public async Task GenerateDeepLinkAsync_ListingUrl_ShouldKeepOriginalPath()
    {
        var link = await CreateStrategy().GenerateDeepLinkAsync(
            "https://produto.mercadolivre.com.br/MLB-2062189012-item-_JM?matt_tool=111&searchVariation=9#position=2",
            Guid.NewGuid(),
            "aff-10");

        Assert.StartsWith("https://produto.mercadolivre.com.br/MLB-2062189012-item-_JM?searchVariation=9&", link, StringComparison.Ordinal);
        Assert.DoesNotContain("#", link, StringComparison.Ordinal);
        Assert.DoesNotContain("matt_tool=111", link, StringComparison.Ordinal);
        AssertMattParams(link);
    }

    [Fact]
    public async Task GenerateDeepLinkAsync_ShowcaseWithoutProduct_ShouldFailInsteadOfUsingAFakeId()
    {
        var error = await Assert.ThrowsAsync<AffiliateLinkGenerationException>(() =>
            CreateStrategy().GenerateDeepLinkAsync(
                "https://www.mercadolivre.com.br/social/matosadalzira?matt_word=lumiaraujo&matt_tool=14343296&forceInApp=true",
                Guid.NewGuid(),
                "aff-10"));

        Assert.Equal(MercadoLivreProductUrlParser.ShowcaseWithoutProductMessage, error.Message);
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
            NullLogger<MercadoLivreLinkStrategy>.Instance);

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
