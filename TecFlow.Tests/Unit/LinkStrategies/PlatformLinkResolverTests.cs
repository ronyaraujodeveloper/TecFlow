using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TecFlow.Business.Integrations;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Core.Enums;

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
    [InlineData("sualoja-20", false)]
    public void IsShortenerUrl_ShouldRecognizeTikTokMagaluAndMercadoLivre(string url, bool expected)
    {
        Assert.Equal(expected, AffiliateTrackingIdValidator.IsShortenerUrl(url));
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
    public void ExtractAffiliateId_ShouldPreferTikTokHandleOverTtFrom()
    {
        var id = PlatformLinkResolver.ExtractAffiliateId(
            "https://www.tiktok.com/@achadinhos.aaz/video/1?tt_from=affiliate_share&sec_uid=MS4wLjAB",
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
}
