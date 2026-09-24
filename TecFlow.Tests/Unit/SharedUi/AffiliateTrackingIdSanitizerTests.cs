using TecFlow.Business.Integrations;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Core.Enums;
using TecFlow.SharedUi.Services.Integrations;

namespace TecFlow.Tests.Unit.SharedUi;

public class AffiliateTrackingIdSanitizerTests
{
    [Fact]
    public void Extract_ShouldKeepPlainId()
    {
        var id = AffiliateTrackingIdSanitizer.Extract(MarketplaceType.Shopee, " 6512300000 ");
        Assert.Equal("6512300000", id);
    }

    [Fact]
    public void Extract_ShouldReadAmazonTagFromUrl()
    {
        var id = AffiliateTrackingIdSanitizer.Extract(
            MarketplaceType.Amazon,
            "https://www.amazon.com.br/dp/B0TESTASIN?tag=sualoja-20&psc=1");

        Assert.Equal("sualoja-20", id);
    }

    [Fact]
    public void Extract_ShouldReadTagQueryWithoutHost()
    {
        var id = AffiliateTrackingIdSanitizer.Extract(MarketplaceType.Amazon, "tag=sualoja-20");
        Assert.Equal("sualoja-20", id);
    }

    [Fact]
    public void Extract_ShouldReadSubIdFromTikTokUrl()
    {
        var id = AffiliateTrackingIdSanitizer.Extract(
            MarketplaceType.TikTokShop,
            "https://shop.tiktok.com/view/product/123?sub_id=12345");

        Assert.Equal("12345", id);
    }

    [Fact]
    public void Extract_ShouldReadMattToolFromMercadoLivreUrl()
    {
        var id = AffiliateTrackingIdSanitizer.Extract(
            MarketplaceType.MercadoLivre,
            "https://www.mercadolivre.com.br/p/MLB123?matt_tool=987654321&matt_word=loja");

        Assert.Equal("987654321", id);
    }

    [Fact]
    public void Extract_ShouldReadParceiroFromCasasBahiaUrl()
    {
        var id = AffiliateTrackingIdSanitizer.Extract(
            MarketplaceType.CasasBahia,
            "https://www.casasbahia.com.br/p/123?parceiro=tecflow_cb&sub_id=loja");

        Assert.Equal("tecflow_cb", id);
    }

    [Fact]
    public void Extract_ShouldReadMagazineVoceSlugFromPath()
    {
        var id = AffiliateTrackingIdSanitizer.Extract(
            MarketplaceType.MagazineLuiza,
            "https://www.magazinevoce.com.br/magazinematos/p/123456/");

        Assert.Equal("magazinematos", id);
    }

    [Fact]
    public void Extract_ShouldPreferPlatformKeyWhenSeveralArePresent()
    {
        var id = AffiliateTrackingIdSanitizer.Extract(
            MarketplaceType.Amazon,
            "https://example.com/?sub_id=12345&tag=minhatag-20");

        Assert.Equal("minhatag-20", id);
    }

    [Fact]
    public void ExtractAffiliateIdFromUrl_ShouldReadShopeeAnIdNumericSequence()
    {
        var id = AffiliateTrackingIdSanitizer.ExtractAffiliateIdFromUrl(
            "https://shopee.com.br/produto?an_id=6512300000&utm_source=affiliate",
            "Shopee");

        Assert.Equal("6512300000", id);
    }

    [Fact]
    public void ExtractAffiliateIdFromUrl_ShouldReadShopeeMmpPidAnPrefix()
    {
        var id = AffiliateTrackingIdSanitizer.ExtractAffiliateIdFromUrl(
            "https://shopee.com.br/product/123/456?mmp_pid=an_18325850271",
            "Shopee");

        Assert.Equal("18325850271", id);
        Assert.Equal(
            "✅ ID de Afiliado 18325850271 extraído com sucesso a partir do link encurtado!",
            AffiliateTrackingIdSanitizer.ExtractedFromShortLinkMessage(id));
    }

    [Fact]
    public void ExtractAffiliateIdFromUrl_ShouldReadShopeeUtmSourceAnPrefix()
    {
        var id = AffiliateTrackingIdSanitizer.ExtractAffiliateIdFromUrl(
            "https://shopee.com.br/product/123/456?utm_source=an_18325850271",
            "Shopee");

        Assert.Equal("18325850271", id);
    }

    [Fact]
    public void PlatformLinkResolver_ShouldExtractShopeeAffiliateIdFromMmpPid()
    {
        var id = PlatformLinkResolver.ExtractShopeeAffiliateId(
            "https://shopee.com.br/product/abc?sub_id=999&mmp_pid=an_18325850271");
        Assert.Equal("18325850271", id);
    }

    [Fact]
    public void ExtractAffiliateIdFromUrl_ShouldReadMattWordWhenMattToolIsMissing()
    {
        var id = AffiliateTrackingIdSanitizer.ExtractAffiliateIdFromUrl(
            "https://www.mercadolivre.com.br/p/MLB123?matt_word=minhaloja",
            "Mercado Livre");

        Assert.Equal("minhaloja", id);
    }

    [Fact]
    public void ExtractAffiliateIdFromUrl_ShouldReadMagaluSubId()
    {
        var id = AffiliateTrackingIdSanitizer.ExtractAffiliateIdFromUrl(
            "https://www.magazineluiza.com.br/p/123/?sub_id=magalu_parceiro",
            "Magalu");

        Assert.Equal("magalu_parceiro", id);
    }

    [Fact]
    public void ExtractAffiliateIdFromUrl_ShouldKeepPlainIdUnchanged()
    {
        var id = AffiliateTrackingIdSanitizer.ExtractAffiliateIdFromUrl("sualoja-20", "Amazon");
        Assert.Equal("sualoja-20", id);
    }

    [Fact]
    public void TryNormalize_ShouldRejectBareUrlWithoutTrackingParams()
    {
        var ok = AffiliateTrackingIdSanitizer.TryNormalize(
            MarketplaceType.Amazon,
            "https://www.amazon.com.br/dp/B0TESTASIN",
            out var id);

        Assert.False(ok);
        Assert.Contains("://", id);
        Assert.Equal(AffiliateTrackingIdSanitizer.InvalidMessage, AffiliateTrackingIdValidator.InvalidMessage);
    }

    [Fact]
    public void IsShortenerUrl_ShouldDetectBrShpEeAndAmznTo()
    {
        Assert.True(AffiliateTrackingIdSanitizer.IsShortenerUrl("https://br.shp.ee/taeej22s"));
        Assert.True(AffiliateTrackingIdSanitizer.IsShortenerUrl("https://shope.ee/abc"));
        Assert.True(AffiliateTrackingIdSanitizer.IsShortenerUrl("https://s.shopee.com.br/xyz"));
        Assert.True(AffiliateTrackingIdSanitizer.IsShortenerUrl("s.shopee.com.br/xyz"));
        Assert.True(AffiliateTrackingIdSanitizer.IsShortenerUrl("https://amzn.to/abc123"));
        Assert.True(AffiliateTrackingIdSanitizer.IsShortenerUrl("https://magalu.me/xyz"));
        Assert.False(AffiliateTrackingIdSanitizer.IsShortenerUrl("sualoja-20"));
    }

    [Fact]
    public void Help_ShouldExposeOfficialPanelForAmazon()
    {
        Assert.Equal("https://associados.amazon.com.br/", AffiliateTrackingIdHelp.GetOfficialPanelUrl(MarketplaceType.Amazon));
        Assert.Contains("-20", AffiliateTrackingIdHelp.GetFormatHint(MarketplaceType.Amazon), StringComparison.Ordinal);
    }
}
