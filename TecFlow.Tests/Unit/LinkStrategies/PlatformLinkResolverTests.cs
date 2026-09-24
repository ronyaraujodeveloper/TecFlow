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
    public void ExtractAffiliateId_ShouldPreferTikTokTtFromOverHandle()
    {
        var id = PlatformLinkResolver.ExtractAffiliateId(
            "https://www.tiktok.com/@loja/video/1?tt_from=affiliate_share&sec_uid=MS4wLjAB",
            MarketplaceType.TikTokShop);

        Assert.Equal("affiliate_share", id);
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
}
