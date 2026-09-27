using System.Net;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TecFlow.Business.Integrations;
using TecFlow.Business.Integrations.Amazon;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Application;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Core.Enums;
using TecFlow.Database.Entity;
using TecFlow.Infrastructure.Services.LinkStrategies;
using TecFlow.Tests.Helpers;

namespace TecFlow.Tests.Unit.LinkStrategies;

public class PlatformLinkResolverTests
{
    [Theory]
    [InlineData("https://vt.tiktok.com/ZSabcde/", true)]
    [InlineData("https://vm.tiktok.com/xyz", true)]
    [InlineData("https://magazineluiza.onelink.me/abc", true)]
    [InlineData("https://magalu.me/xyz", true)]
    [InlineData("https://www.magazinevoce.com.br/minhaloja/p/1", true)]
    [InlineData("https://meli.la/abc123", true)]
    [InlineData("https://www.mercadolivre.com/sec/abc", true)]
    [InlineData("https://www.mercadolivre.com.br/sec/xyz", true)]
    [InlineData("https://ofertou.ai/UjGXJ", true)]
    [InlineData("https://ofertou.ai/drXB-Magalu", true)]
    [InlineData("https://promoby.me/6nf9k3d5", true)]
    [InlineData("https://bit.ly/abc", true)]
    [InlineData("sualoja-20", false)]
    public void IsShortenerUrl_ShouldRecognizeTikTokMagaluAndMercadoLivre(string url, bool expected)
    {
        Assert.Equal(expected, AffiliateTrackingIdValidator.IsShortenerUrl(url));
    }

    [Fact]
    public void ExtractAffiliateId_ShouldReadTikTokUniqueIdFromEncodedLoginRedirect()
    {
        const string destination =
            "https://shop.tiktok.com/view/product/1?unique_id=amz.indica&user_id=7426532104848278533";
        var encodedOnce = Uri.EscapeDataString(destination);
        var encodedTwice = Uri.EscapeDataString(encodedOnce);
        var loginUrl = $"https://www.tiktok.com/login?redirect_url={encodedTwice}";

        var unwrapped = AffiliateTrackingIdValidator.UnwrapTikTokLoginRedirect(loginUrl);
        Assert.Contains("unique_id=amz.indica", unwrapped, StringComparison.OrdinalIgnoreCase);

        var id = PlatformLinkResolver.ExtractAffiliateId(loginUrl, MarketplaceType.TikTokShop);

        Assert.Equal("amz.indica", id);
    }

    [Theory]
    [InlineData("amz.indica")]
    [InlineData("@amz.indica")]
    public void ExtractAffiliateId_ShouldAcceptTikTokBareHandle(string input)
    {
        var id = PlatformLinkResolver.ExtractAffiliateId(input, MarketplaceType.TikTokShop);
        Assert.Equal("amz.indica", id);
        Assert.True(AffiliateTrackingIdValidator.TryNormalize(MarketplaceType.TikTokShop, input, out var normalized));
        Assert.Equal("amz.indica", normalized);
    }

    [Fact]
    public void ExtractAffiliateId_ShouldReadTikTokUniqueIdAmzIndica()
    {
        const string url =
            "https://shop.tiktok.com/view/product/1?unique_id=amz.indica&user_id=7426532104848278533&sec_user_id=MS4wLjAB";

        var id = PlatformLinkResolver.ExtractAffiliateId(url, MarketplaceType.TikTokShop);

        Assert.Equal("amz.indica", id);
        Assert.Equal(
            "amz.indica",
            PlatformLinkResolver.ExtractAffiliateId(
                "https://vt.tiktok.com/ZSabc/?unique_id=@amz.indica",
                MarketplaceType.TikTokShop));
        Assert.True(PlatformLinkResolver.TryDetectPlatformFromUrl("https://vt.tiktok.com/ZSabc/", out var fromVt));
        Assert.Equal(MarketplaceType.TikTokShop, fromVt);
        Assert.True(PlatformLinkResolver.TryDetectPlatformFromUrl("https://shop.tiktok.com/view/product/1", out var fromShop));
        Assert.Equal(MarketplaceType.TikTokShop, fromShop);
    }

    [Fact]
    public void ExtractAffiliateId_ShouldReadTikTokUserIdWhenUniqueIdIsMissing()
    {
        Assert.Equal(
            "7426532104848278533",
            PlatformLinkResolver.ExtractAffiliateId(
                "https://shop.tiktok.com/view/product/1?user_id=7426532104848278533&sec_user_id=MS4wLjAB",
                MarketplaceType.TikTokShop));
        Assert.Equal(
            "MS4wLjABAAAAHASH",
            PlatformLinkResolver.ExtractAffiliateId(
                "https://shop.tiktok.com/view/product/1?sec_user_id=MS4wLjABAAAAHASH",
                MarketplaceType.TikTokShop));
    }

    [Fact]
    public void ExtractAffiliateId_ShouldReadTikTokHandleAfterAt()
    {
        var id = PlatformLinkResolver.ExtractAffiliateId(
            "https://www.tiktok.com/@achadinhos.aaz/video/123",
            MarketplaceType.TikTokShop);

        Assert.Equal("achadinhos.aaz", id);
    }

    [Fact]
    public void ExtractAffiliateId_ShouldPreferTikTokUniqueIdOverHandle()
    {
        var id = PlatformLinkResolver.ExtractAffiliateId(
            "https://www.tiktok.com/@achadinhos.aaz/video/1?unique_id=amz.indica&sec_uid=MS4wLjAB",
            MarketplaceType.TikTokShop);

        Assert.Equal("amz.indica", id);
    }

    [Fact]
    public void ExtractAffiliateId_ShouldPreferTikTokHandleOverTtFrom()
    {
        var id = PlatformLinkResolver.ExtractAffiliateId(
            "https://www.tiktok.com/@achadinhos.aaz/video/1?tt_from=affiliate_share",
            MarketplaceType.TikTokShop);

        Assert.Equal("achadinhos.aaz", id);
    }

    [Fact]
    public void ExtractAffiliateId_ShouldReadTikTokTtFromWhenHandleIsMissing()
    {
        var id = PlatformLinkResolver.ExtractAffiliateId(
            "https://www.tiktok.com/t/abc?tt_from=creator123",
            MarketplaceType.TikTokShop);

        Assert.Equal("creator123", id);
    }

    [Fact]
    public void ExtractAffiliateId_ShouldReadTikTokSecUid()
    {
        var id = PlatformLinkResolver.ExtractAffiliateId(
            "https://www.tiktok.com/t/abc?sec_uid=MS4wLjABAAAA123",
            MarketplaceType.TikTokShop);

        Assert.Equal("MS4wLjABAAAA123", id);
    }

    [Fact]
    public void ExtractAffiliateId_ShouldReadMagaluPromoterId5321952()
    {
        const string url =
            "https://www.magazineluiza.com.br/produto/?partner_id=3440&promoter_id=5321952&utm_source=divulgador";

        var id = PlatformLinkResolver.ExtractAffiliateId(url, MarketplaceType.MagazineLuiza);

        Assert.Equal("5321952", id);
        Assert.True(PlatformLinkResolver.TryDetectPlatformFromUrl(
            "https://magazineluiza.onelink.me/abc?promoter_id=5321952",
            out var platform));
        Assert.Equal(MarketplaceType.MagazineLuiza, platform);
        Assert.Equal(
            "5321952",
            PlatformLinkResolver.ExtractAffiliateId(
                "https://magazineluiza.onelink.me/abc?promoter_id=5321952&utm_source=magalu",
                MarketplaceType.MagazineLuiza));
    }

    [Fact]
    public void ExtractAffiliateId_ShouldReadNumericUtmCampaignWhenPromoterIdIsMissing()
    {
        var id = PlatformLinkResolver.ExtractAffiliateId(
            "https://www.magazineluiza.com.br/p/1/?utm_campaign=5321952&utm_source=divulgador",
            MarketplaceType.MagazineLuiza);

        Assert.Equal("5321952", id);
    }

    [Fact]
    public void ExtractAffiliateId_ShouldReadMagazineVoceStoreSlug()
    {
        var id = PlatformLinkResolver.ExtractAffiliateId(
            "https://www.magazinevoce.com.br/magazinematos/p/218434100/",
            MarketplaceType.MagazineLuiza);

        Assert.Equal("magazinematos", id);
    }

    [Fact]
    public void ExtractAffiliateId_ShouldReadMagaluParceiroAndAfiliado()
    {
        Assert.Equal(
            "loja_parceira",
            PlatformLinkResolver.ExtractAffiliateId(
                "https://www.magazineluiza.com.br/p/123/?parceiro=loja_parceira",
                MarketplaceType.MagazineLuiza));
        Assert.Equal(
            "aff_magalu",
            PlatformLinkResolver.ExtractAffiliateId(
                "https://www.magazineluiza.com.br/p/123/?afiliado=aff_magalu",
                MarketplaceType.MagazineLuiza));
    }

    [Fact]
    public void ExtractAffiliateId_ShouldReadMercadoLivreMattAndPenn()
    {
        Assert.Equal(
            "987654321",
            PlatformLinkResolver.ExtractAffiliateId(
                "https://www.mercadolivre.com.br/p/MLB123?matt_tool=987654321&matt_word=loja",
                MarketplaceType.MercadoLivre));
        Assert.Equal(
            "wordloja",
            PlatformLinkResolver.ExtractAffiliateId(
                "https://www.mercadolivre.com.br/p/MLB123?matt_word=wordloja",
                MarketplaceType.MercadoLivre));
        Assert.Equal(
            "PENN99",
            PlatformLinkResolver.ExtractAffiliateId(
                "https://www.mercadolivre.com/sec/abc?penn=PENN99",
                MarketplaceType.MercadoLivre));
        Assert.Equal(
            "42",
            PlatformLinkResolver.ExtractAffiliateId(
                "https://meli.la/xyz?penn_id=42",
                MarketplaceType.MercadoLivre));
    }

    [Fact]
    public void ExtractedCredentialMessage_ShouldNamePlatform()
    {
        Assert.Equal(
            "✅ Credencial achadinhos.aaz extraída com sucesso para TikTok Shop!",
            AffiliateTrackingIdValidator.ExtractedCredentialMessage("achadinhos.aaz", MarketplaceType.TikTokShop));
        Assert.Equal(
            "✅ Credencial magazinematos extraída com sucesso para Magazine Luiza!",
            AffiliateTrackingIdValidator.ExtractedCredentialMessage("magazinematos", MarketplaceType.MagazineLuiza));
        Assert.Equal(
            "✅ Credencial 987654321 extraída com sucesso para Mercado Livre!",
            AffiliateTrackingIdValidator.ExtractedCredentialMessage("987654321", MarketplaceType.MercadoLivre));
    }

    [Fact]
    public async Task ExpandIfShortenedAsync_ShouldExpandTikTokMagaluAndMercadoLivreShorteners()
    {
        var expansion = new Mock<IUrlExpansionService>();
        expansion.Setup(service => service.ExpandUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string url, CancellationToken _) => url + "-expanded");

        var resolver = new PlatformLinkResolver(
            Array.Empty<IPlatformLinkStrategy>(),
            NullLogger<PlatformLinkResolver>.Instance,
            expansion.Object);

        Assert.Equal(
            "https://vt.tiktok.com/ZSabcde/-expanded",
            await resolver.ExpandIfShortenedAsync("https://vt.tiktok.com/ZSabcde/"));
        Assert.Equal(
            "https://magazineluiza.onelink.me/abc-expanded",
            await resolver.ExpandIfShortenedAsync("https://magazineluiza.onelink.me/abc"));
        Assert.Equal(
            "https://meli.la/xyz-expanded",
            await resolver.ExpandIfShortenedAsync("https://meli.la/xyz"));
        expansion.Verify(
            service => service.ExpandUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Theory]
    [InlineData("https://vt.tiktok.com/ZSabcde/", MarketplaceType.TikTokShop)]
    [InlineData("https://www.tiktok.com/@loja", MarketplaceType.TikTokShop)]
    [InlineData("https://magazineluiza.onelink.me/abc", MarketplaceType.MagazineLuiza)]
    [InlineData("https://magalu.me/xyz", MarketplaceType.MagazineLuiza)]
    [InlineData("https://www.magazinevoce.com.br/minhaloja/p/1", MarketplaceType.MagazineLuiza)]
    [InlineData("https://meli.la/abc123", MarketplaceType.MercadoLivre)]
    [InlineData("https://www.mercadolivre.com/sec/abc", MarketplaceType.MercadoLivre)]
    [InlineData("https://s.shopee.com.br/abc", MarketplaceType.Shopee)]
    [InlineData("https://br.shp.ee/abc", MarketplaceType.Shopee)]
    [InlineData("https://shopee.com.br/produto", MarketplaceType.Shopee)]
    [InlineData("https://amzn.to/abc", MarketplaceType.Amazon)]
    [InlineData("https://www.amazon.com.br/dp/B0TEST", MarketplaceType.Amazon)]
    [InlineData("https://cb.com.br/p/1", MarketplaceType.CasasBahia)]
    [InlineData("https://www.casasbahia.com.br/p/1", MarketplaceType.CasasBahia)]
    [InlineData("https://kb.um/abc", MarketplaceType.Kabum)]
    [InlineData("https://www.kabum.com.br/produto/1", MarketplaceType.Kabum)]
    public void TryDetectPlatformFromUrl_ShouldMapKnownDomains(string url, MarketplaceType expected)
    {
        Assert.True(PlatformLinkResolver.TryDetectPlatformFromUrl(url, out var platform));
        Assert.Equal(expected, platform);
    }

    [Fact]
    public void ExtractAffiliateId_ShouldNotReadTrueFromMagaluOnelinkAfDp()
    {
        var id = PlatformLinkResolver.ExtractAffiliateId(
            "https://magazineluiza.onelink.me/abc?pid=app&af_dp=true&parceiro=magazinevoce",
            MarketplaceType.MagazineLuiza);

        Assert.Equal("magazinevoce", id);
        Assert.NotEqual("true", id);
    }

    [Fact]
    public void PreferExtractedCredential_ShouldIgnoreBooleanLiteral()
    {
        var value = AffiliateTrackingIdValidator.PreferExtractedCredential(
            "true",
            "https://www.magazinevoce.com.br/minhaloja/p/1",
            "fallback");

        Assert.Equal("https://www.magazinevoce.com.br/minhaloja/p/1", value);
    }

    [Fact]
    public void UrlUnshortener_ShouldParseHtmlAndStripForeignTracking()
    {
        var fromJs = UrlUnshortenerService.TryExtractRedirectFromHtml(
            "<script>window.location.href = 'https://www.kabum.com.br/produto/123456?aff_id=999&utm_source=ofertou';</script>",
            "https://ofertou.ai/UjGXJ");
        Assert.Equal(
            "https://www.kabum.com.br/produto/123456?aff_id=999&utm_source=ofertou",
            fromJs);
        Assert.Equal(
            "https://www.kabum.com.br/produto/123456",
            UrlUnshortenerService.StripForeignTracking(fromJs));

        var fromMeta = UrlUnshortenerService.TryExtractRedirectFromHtml(
            """<meta http-equiv="refresh" content="0;url=https://www.amazon.com.br/dp/B08N5WRWNW?tag=other-20">""",
            "https://promoby.me/6nf9k3d5");
        Assert.Contains("B08N5WRWNW", fromMeta, StringComparison.Ordinal);
        Assert.Equal(
            "https://www.amazon.com.br/dp/B08N5WRWNW",
            UrlUnshortenerService.StripForeignTracking(fromMeta));
        Assert.True(UrlUnshortenerService.TryDetectMarketplace(
            "https://www.kabum.com.br/produto/123456",
            out var kabum));
        Assert.Equal(MarketplaceType.Kabum, kabum);
    }

    [Fact]
    public async Task ExpandIfShortenedAsync_ShouldResolveOfertouAndPromobyAggregatorsToTenantLinks()
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

        var expansion = new UrlExpansionService(
            new StubHttpClientFactory(handler),
            NullLogger<UrlExpansionService>.Instance);

        var kabumStore = CreateTenantStore(MarketplaceType.Kabum, "tecflow_kabum");
        var magaluStore = CreateTenantStore(MarketplaceType.MagazineLuiza, "5321952");
        var amazonStore = CreateTenantStore(MarketplaceType.Amazon, "sualoja-20");

        var resolver = new PlatformLinkResolver(
            [
                CreateKabumStrategy(expansion, kabumStore),
                CreateMagaluStrategy(expansion, magaluStore),
                CreateAmazonStrategy(expansion, amazonStore)
            ],
            NullLogger<PlatformLinkResolver>.Instance,
            expansion);

        var kabumExpanded = await resolver.ExpandIfShortenedAsync("https://ofertou.ai/UjGXJ");
        Assert.Equal("https://www.kabum.com.br/produto/123456", kabumExpanded);
        Assert.DoesNotContain("aff_id", kabumExpanded, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("utm_source", kabumExpanded, StringComparison.OrdinalIgnoreCase);
        Assert.True(UrlUnshortenerService.TryDetectMarketplace(kabumExpanded, out var kabumPlatform));
        Assert.Equal(MarketplaceType.Kabum, kabumPlatform);
        Assert.Equal(MarketplaceType.Kabum, resolver.Resolve(kabumExpanded).PlatformType);
        Assert.Equal(
            "https://www.kabum.com.br/produto/123456?sub_id=tecflow_kabum&utm_source=afiliado",
            await resolver.Resolve(kabumExpanded).GenerateDeepLinkAsync(kabumExpanded, Guid.NewGuid(), "10"));
        Assert.Equal(
            "https://www.kabum.com.br/produto/123456?sub_id=tecflow_kabum&utm_source=afiliado",
            UrlUnshortenerService.ApplyTenantCredentials(kabumExpanded, "tecflow_kabum"));

        var magaluExpanded = await resolver.ExpandIfShortenedAsync("https://ofertou.ai/drXB-Magalu");
        Assert.StartsWith("https://www.magazineluiza.com.br/smart-tv-50-tcl", magaluExpanded, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("partner_id", magaluExpanded, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("promoter_id=3223826", magaluExpanded, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("utm_campaign=3223826", magaluExpanded, StringComparison.OrdinalIgnoreCase);
        Assert.True(UrlUnshortenerService.TryDetectMarketplace(magaluExpanded, out var magaluPlatform));
        Assert.Equal(MarketplaceType.MagazineLuiza, magaluPlatform);
        Assert.Equal(
            "Smart TV 50 TCL 4K UHD QLED 50P7K Google TV AIPQ Google Assistente 3 HDMI",
            ProductMetadataHtmlParser.TryExtractMarketplaceProductNameFromUrl(magaluExpanded));
        Assert.Equal(
            "https://www.magazineluiza.com.br/p/218434100/?promoter_id=5321952",
            await resolver.Resolve(magaluExpanded).GenerateDeepLinkAsync(magaluExpanded, Guid.NewGuid(), "10"));
        Assert.Equal(
            "https://www.magazineluiza.com.br/p/218434100/?promoter_id=5321952",
            UrlUnshortenerService.ApplyTenantCredentials(magaluExpanded, "5321952"));

        var amazonExpanded = await resolver.ExpandIfShortenedAsync("https://promoby.me/6nf9k3d5");
        Assert.Contains("amazon.com.br", amazonExpanded, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("https://www.amazon.com.br/dp/B08N5WRWNW", amazonExpanded);
        Assert.DoesNotContain("tag=", amazonExpanded, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("promobit-d-20", amazonExpanded, StringComparison.OrdinalIgnoreCase);
        Assert.True(UrlUnshortenerService.TryDetectMarketplace(amazonExpanded, out var amazonPlatform));
        Assert.Equal(MarketplaceType.Amazon, amazonPlatform);
        Assert.Equal(
            "https://www.amazon.com.br/dp/B08N5WRWNW?tag=sualoja-20",
            await resolver.Resolve(amazonExpanded).GenerateDeepLinkAsync(amazonExpanded, Guid.NewGuid(), "10"));
    }

    [Fact]
    public async Task ExpandIfShortenedAsync_ShouldExpandPromobyMeToAmazonComBr()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            if ((request.RequestUri?.Host ?? string.Empty)
                .Contains("promoby.me", StringComparison.OrdinalIgnoreCase))
            {
                return Html(
                    """<html><head><meta http-equiv="refresh" content="0;url=https://www.amazon.com.br/dp/B0C4BW38R4?tag=promobit-d-20"></head></html>""");
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var expansion = new UrlExpansionService(
            new StubHttpClientFactory(handler),
            NullLogger<UrlExpansionService>.Instance);
        var amazonStore = CreateTenantStore(MarketplaceType.Amazon, "sualoja-20");
        var resolver = new PlatformLinkResolver(
            [CreateAmazonStrategy(expansion, amazonStore)],
            NullLogger<PlatformLinkResolver>.Instance,
            expansion);

        Exception? thrown = null;
        IPlatformLinkStrategy? strategy = null;
        var destination = string.Empty;
        try
        {
            (strategy, destination) = await resolver.ResolveFromInputAsync("https://promoby.me/6nf9k3d5");
        }
        catch (Exception ex)
        {
            thrown = ex;
        }

        Assert.True(thrown is null, thrown?.Message);
        Assert.DoesNotContain(
            ShortAffiliateLinkService.UnrecognizedDestinationMessage,
            thrown?.Message ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains("amazon.com.br", destination, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("B0C4BW38R4", destination, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("promobit-d-20", destination, StringComparison.OrdinalIgnoreCase);
        Assert.True(ShortAffiliateLinkService.IsSupportedDomain(destination));
        Assert.Equal(MarketplaceType.Amazon, strategy!.PlatformType);
        Assert.True(AmazonProductUrlParser.TryParse(destination, out var asin));
        Assert.Equal("B0C4BW38R4", asin);
        Assert.Equal("B0C4BW38R4", ProductMetadataHtmlParser.TryExtractMarketplaceProductNameFromUrl(destination));
        Assert.Equal(
            "https://www.amazon.com.br/dp/B0C4BW38R4?tag=sualoja-20",
            await strategy.GenerateDeepLinkAsync(destination, Guid.NewGuid(), "10"));
    }

    private static HttpResponseMessage Html(string body) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "text/html")
        };

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

    private static IPlatformLinkStrategy CreateKabumStrategy(IUrlExpansionService expansion, IntegracaoLoja store) =>
        new KabumLinkStrategy(
            expansion,
            new FixedTenantStoreResolver(store),
            new AffiliateLinkGenerationContext { UserId = 10 },
            CreateHostEnvironment(),
            NullLogger<KabumLinkStrategy>.Instance);

    private static IPlatformLinkStrategy CreateMagaluStrategy(IUrlExpansionService expansion, IntegracaoLoja store) =>
        new MagazineLuizaLinkStrategy(
            expansion,
            new FixedTenantStoreResolver(store),
            new AffiliateLinkGenerationContext { UserId = 10 },
            CreateHostEnvironment(),
            NullLogger<MagazineLuizaLinkStrategy>.Instance);

    private static IPlatformLinkStrategy CreateAmazonStrategy(IUrlExpansionService expansion, IntegracaoLoja store) =>
        new AmazonLinkStrategy(
            expansion,
            new FixedTenantStoreResolver(store),
            new AffiliateLinkGenerationContext { UserId = 10 },
            CreateHostEnvironment(),
            NullLogger<AmazonLinkStrategy>.Instance);

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
