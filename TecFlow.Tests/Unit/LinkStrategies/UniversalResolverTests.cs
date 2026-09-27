using System.Net;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Core.Enums;
using TecFlow.Database.Entity;
using TecFlow.Infrastructure.Services.LinkStrategies;
using TecFlow.Tests.Helpers;

namespace TecFlow.Tests.Unit.LinkStrategies;

public class UniversalResolverTests
{
    [Theory]
    [InlineData("https://www.amazon.com.br/dp/B08N5WRWNW", MarketplaceType.Amazon)]
    [InlineData("https://amzn.to/abc", MarketplaceType.Amazon)]
    [InlineData("https://amzn.br/abc", MarketplaceType.Amazon)]
    [InlineData("https://www.magazineluiza.com.br/p/1", MarketplaceType.MagazineLuiza)]
    [InlineData("https://www.magazinevoce.com.br/loja/p/1", MarketplaceType.MagazineLuiza)]
    [InlineData("https://magalu.me/xyz", MarketplaceType.MagazineLuiza)]
    [InlineData("https://www.kabum.com.br/produto/123", MarketplaceType.Kabum)]
    [InlineData("https://shopee.com.br/item", MarketplaceType.Shopee)]
    [InlineData("https://s.shopee.com.br/abc", MarketplaceType.Shopee)]
    [InlineData("https://br.shp.ee/abc", MarketplaceType.Shopee)]
    [InlineData("https://www.mercadolivre.com.br/p/MLB1", MarketplaceType.MercadoLivre)]
    [InlineData("https://www.mercadolibre.com/p/MLB1", MarketplaceType.MercadoLivre)]
    [InlineData("https://meli.la/abc", MarketplaceType.MercadoLivre)]
    [InlineData("https://www.casasbahia.com.br/p/1", MarketplaceType.CasasBahia)]
    [InlineData("https://www.tiktok.com/@loja", MarketplaceType.TikTokShop)]
    public void TryMapDomainToPlatform_ShouldMapKnownHosts(string url, MarketplaceType expected)
    {
        Assert.True(UniversalLinkResolverEngine.TryMapDomainToPlatform(url, out var platform));
        Assert.Equal(expected, platform);
        Assert.Equal(expected.ToString(), ProductMetadataService.ResolvePlatform(url));
    }

    [Fact]
    public void StripCommissionAndTracking_ShouldRemovePidCAndSubid()
    {
        var cleaned = UniversalLinkResolverEngine.StripCommissionAndTracking(
            "https://www.amazon.com.br/dp/B08N5WRWNW?tag=promobit-d-20&pid=x&c=1&subid=z&utm_source=promo");

        Assert.Equal("https://www.amazon.com.br/dp/B08N5WRWNW", cleaned);
        Assert.DoesNotContain("promobit-d-20", cleaned, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResolveFinalDestinationUrlAsync_ShouldExpandOfertouMagaluAndInject5321952()
    {
        var engine = CreateEngine();

        var expanded = await engine.ResolveFinalDestinationUrlAsync("https://ofertou.ai/drXB-Magalu");

        Assert.True(UniversalLinkResolverEngine.TryMapDomainToPlatform(expanded, out var platform));
        Assert.Equal(MarketplaceType.MagazineLuiza, platform);
        Assert.DoesNotContain("partner_id", expanded, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("3223826", expanded, StringComparison.Ordinal);
        Assert.Equal(
            "https://www.magazineluiza.com.br/p/218434100/?promoter_id=5321952",
            UniversalLinkResolverEngine.ApplyTenantCredentials(expanded, "5321952"));
    }

    [Fact]
    public async Task ResolveFinalDestinationUrlAsync_ShouldExpandPromobyAmazonAndReplacePromobitTag()
    {
        var engine = CreateEngine();

        var expanded = await engine.ResolveFinalDestinationUrlAsync("https://promoby.me/6nf9k3d5");

        Assert.True(UniversalLinkResolverEngine.TryMapDomainToPlatform(expanded, out var platform));
        Assert.Equal(MarketplaceType.Amazon, platform);
        Assert.DoesNotContain("promobit-d-20", expanded, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tag=", expanded, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(
            "https://www.amazon.com.br/dp/B08N5WRWNW?tag=sualoja-20",
            UniversalLinkResolverEngine.ApplyTenantCredentials(expanded, "sualoja-20"));
    }

    [Fact]
    public async Task ResolveFinalDestinationUrlAsync_ShouldExpandOfertouKabumAndInjectTenantSubId()
    {
        var engine = CreateEngine();

        var expanded = await engine.ResolveFinalDestinationUrlAsync("https://ofertou.ai/UjGXJ");

        Assert.True(UniversalLinkResolverEngine.TryMapDomainToPlatform(expanded, out var platform));
        Assert.Equal(MarketplaceType.Kabum, platform);
        Assert.DoesNotContain("aff_id", expanded, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(
            "https://www.kabum.com.br/produto/123456?sub_id=tecflow_kabum&utm_source=afiliado",
            UniversalLinkResolverEngine.ApplyTenantCredentials(expanded, "tecflow_kabum"));
    }

    [Fact]
    public async Task PlatformLinkResolver_ShouldUseUniversalEngineForAggregatorUrls()
    {
        var expansion = CreateExpansion();
        var engine = new UniversalLinkResolverEngine(expansion);
        var resolver = new PlatformLinkResolver(
            [
                CreateKabumStrategy(expansion),
                CreateMagaluStrategy(expansion),
                CreateAmazonStrategy(expansion)
            ],
            NullLogger<PlatformLinkResolver>.Instance,
            expansion,
            engine);

        var magalu = await resolver.ExpandIfShortenedAsync("https://ofertou.ai/drXB-Magalu");
        Assert.Equal(MarketplaceType.MagazineLuiza, resolver.Resolve(magalu).PlatformType);
        Assert.Equal(
            "https://www.magazineluiza.com.br/p/218434100/?promoter_id=5321952",
            await resolver.Resolve(magalu).GenerateDeepLinkAsync(magalu, Guid.NewGuid(), "10"));

        var amazon = await resolver.ExpandIfShortenedAsync("https://promoby.me/6nf9k3d5");
        Assert.DoesNotContain("promobit-d-20", amazon, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(
            "https://www.amazon.com.br/dp/B08N5WRWNW?tag=sualoja-20",
            await resolver.Resolve(amazon).GenerateDeepLinkAsync(amazon, Guid.NewGuid(), "10"));

        var kabum = await resolver.ExpandIfShortenedAsync("https://ofertou.ai/UjGXJ");
        Assert.Equal(
            "https://www.kabum.com.br/produto/123456?sub_id=tecflow_kabum&utm_source=afiliado",
            await resolver.Resolve(kabum).GenerateDeepLinkAsync(kabum, Guid.NewGuid(), "10"));
    }

    private static UniversalLinkResolverEngine CreateEngine() =>
        new(CreateExpansion());

    private static UrlExpansionService CreateExpansion()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            var host = request.RequestUri?.Host ?? string.Empty;
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;

            if (host.Contains("ofertou.ai", StringComparison.OrdinalIgnoreCase)
                && path.Contains("UjGXJ", StringComparison.OrdinalIgnoreCase))
            {
                return Html(
                    "<html><script>window.location.href = \"https://www.kabum.com.br/produto/123456?aff_id=999&utm_source=ofertou&utm_medium=cpc\";</script></html>");
            }

            if (host.Contains("ofertou.ai", StringComparison.OrdinalIgnoreCase)
                && path.Contains("drXB-Magalu", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.Found)
                {
                    Headers =
                    {
                        Location = new Uri(
                            "https://www.magazineluiza.com.br/smart-tv-50-tcl-4k-uhd-qled-50p7k-google-tv-aipq-google-assistente-3-hdmi/p/218434100/te/tvlc/?partner_id=3440&promoter_id=3223826&utm_campaign=3223826")
                    }
                };
            }

            if (host.Contains("promoby.me", StringComparison.OrdinalIgnoreCase))
            {
                return Html(
                    """<html><head><meta http-equiv="refresh" content="0;url=https://www.amazon.com.br/dp/B08N5WRWNW?tag=promobit-d-20&utm_campaign=promo"></head></html>""");
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        return new UrlExpansionService(
            new StubHttpClientFactory(handler),
            NullLogger<UrlExpansionService>.Instance);
    }

    private static HttpResponseMessage Html(string body) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "text/html")
        };

    private static IPlatformLinkStrategy CreateKabumStrategy(IUrlExpansionService expansion) =>
        new KabumLinkStrategy(
            expansion,
            new FixedTenantStoreResolver(CreateTenantStore(MarketplaceType.Kabum, "tecflow_kabum")),
            new AffiliateLinkGenerationContext { UserId = 10 },
            CreateHostEnvironment(),
            NullLogger<KabumLinkStrategy>.Instance);

    private static IPlatformLinkStrategy CreateMagaluStrategy(IUrlExpansionService expansion) =>
        new MagazineLuizaLinkStrategy(
            expansion,
            new FixedTenantStoreResolver(CreateTenantStore(MarketplaceType.MagazineLuiza, "5321952")),
            new AffiliateLinkGenerationContext { UserId = 10 },
            CreateHostEnvironment(),
            NullLogger<MagazineLuizaLinkStrategy>.Instance);

    private static IPlatformLinkStrategy CreateAmazonStrategy(IUrlExpansionService expansion) =>
        new AmazonLinkStrategy(
            expansion,
            new FixedTenantStoreResolver(CreateTenantStore(MarketplaceType.Amazon, "sualoja-20")),
            new AffiliateLinkGenerationContext { UserId = 10 },
            CreateHostEnvironment(),
            NullLogger<AmazonLinkStrategy>.Instance);

    private static IntegracaoLoja CreateTenantStore(MarketplaceType platform, string trackingId) =>
        new()
        {
            Id = (int)platform + 10,
            UserId = 10,
            TenantId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            ShopId = $"shop-{platform}",
            FriendlyName = platform.GetDisplayName(),
            AffiliateTrackingId = trackingId,
            AccessToken = "token",
            PlatformType = platform
        };

    private static IHostEnvironment CreateHostEnvironment()
    {
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(item => item.EnvironmentName).Returns("Homologacao");
        return environment.Object;
    }

    private sealed class FixedTenantStoreResolver : IIntegracaoLojaScopeResolver
    {
        private readonly IntegracaoLoja _store;

        public FixedTenantStoreResolver(IntegracaoLoja store) => _store = store;

        public Task<IntegracaoLoja> ResolveAsync(
            Guid storeScopeId,
            int userId,
            MarketplaceType expectedPlatform,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_store);
    }
}
