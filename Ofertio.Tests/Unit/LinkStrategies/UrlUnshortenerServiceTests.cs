using Ofertio.Business.Integrations;
using Ofertio.Business.Service.Application;
using Ofertio.Business.Service.LinkStrategies;
using Ofertio.Core.Enums;

namespace Ofertio.Tests.Unit.LinkStrategies;

public class UrlUnshortenerServiceTests
{
    [Fact]
    public async Task ResolveToFinalSupportedMarketplaceAsync_ShouldIterateUntilAmazonComBr()
    {
        var hops = new List<string>();

        var result = await UrlUnshortenerService.ResolveToFinalSupportedMarketplaceAsync(
            "https://promoby.me/6nf9k3d5",
            (url, _) =>
            {
                hops.Add(url);
                if (url.Contains("://promoby.me/", StringComparison.OrdinalIgnoreCase)
                    || url.Contains("://www.promoby.me/", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult<string?>("https://go.promoby.me/track/6nf9k3d5");
                }

                if (url.Contains("go.promoby.me", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult<string?>(
                        "https://www.amazon.com.br/dp/B0C4BW38R4?tag=promobit-d-20&utm_source=promo");
                }

                return Task.FromResult<string?>(null);
            });

        Assert.Equal(2, hops.Count);
        Assert.True(hops.Count <= UrlUnshortenerService.MaxResolutionLoops);
        Assert.Contains("amazon.com.br", result, StringComparison.OrdinalIgnoreCase);
        Assert.True(UrlUnshortenerService.IsSupportedMarketplaceUrl(result));
        Assert.True(UrlUnshortenerService.TryDetectMarketplace(result, out var platform));
        Assert.Equal(MarketplaceType.Amazon, platform);
        Assert.DoesNotContain("promobit-d-20", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResolveAffiliateUrlAsync_ShouldKeepMercadoLivreIdAfterStrippingTheLink()
    {
        var resolved = await UrlUnshortenerService.ResolveAffiliateUrlAsync(
            "https://meli.la/abc123",
            (url, _) =>
            {
                if (url.Contains("meli.la", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult<string?>(
                        "https://www.mercadolivre.com.br/p/MLB4012345678?matt_tool=14343296&matt_word=loja");
                }

                return Task.FromResult<string?>(null);
            });

        Assert.Equal("14343296", resolved.MercadoLivreTrackingId);
        Assert.Contains("mercadolivre.com.br", resolved.Url, StringComparison.OrdinalIgnoreCase);
        Assert.True(AffiliateTrackingIdValidator.TryParseMercadoLivreTrackingId("14343296", out var id));
        Assert.Equal("14343296", id);
        Assert.True(ShortAffiliateLinkService.IsSupportedDomain(resolved.Url));
    }

    [Fact]
    public async Task ResolveToFinalSupportedMarketplaceAsync_ShouldStopAtMagazineluizaComBr()
    {
        var hops = new List<string>();

        var result = await UrlUnshortenerService.ResolveToFinalSupportedMarketplaceAsync(
            "https://ofertou.ai/drXB-Magalu",
            (url, _) =>
            {
                hops.Add(url);
                if (url.Contains("://ofertou.ai/", StringComparison.OrdinalIgnoreCase)
                    || url.Contains("://www.ofertou.ai/", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult<string?>("https://click.ofertou.ai/go/drXB-Magalu");
                }

                if (url.Contains("click.ofertou.ai", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult<string?>(
                        "https://www.magazineluiza.com.br/smart-tv-50/p/218434100/?partner_id=3440&promoter_id=3223826");
                }

                return Task.FromResult<string?>(null);
            });

        Assert.Equal(2, hops.Count);
        Assert.True(hops.Count <= UrlUnshortenerService.MaxResolutionLoops);
        Assert.Contains("magazineluiza.com.br", result, StringComparison.OrdinalIgnoreCase);
        Assert.True(UrlUnshortenerService.IsSupportedMarketplaceUrl(result));
        Assert.True(UrlUnshortenerService.TryDetectMarketplace(result, out var platform));
        Assert.Equal(MarketplaceType.MagazineLuiza, platform);
        Assert.DoesNotContain("partner_id", result, StringComparison.OrdinalIgnoreCase);
        Assert.True(ShortAffiliateLinkService.IsSupportedDomain(result));
    }

    [Fact]
    public async Task ResolveToFinalSupportedMarketplaceAsync_ShouldKeepLastUrlWhenMarketplaceIsNeverFound()
    {
        var hops = 0;
        var result = await UrlUnshortenerService.ResolveToFinalSupportedMarketplaceAsync(
            "https://unknown-offers.example/abc",
            (url, _) =>
            {
                hops++;
                return Task.FromResult<string?>($"https://unknown-offers.example/hop-{hops}");
            });

        Assert.Equal(UrlUnshortenerService.MaxResolutionLoops, hops);
        Assert.False(UrlUnshortenerService.IsSupportedMarketplaceUrl(result));
        Assert.False(ShortAffiliateLinkService.IsSupportedDomain(result));
        Assert.Equal(ShortAffiliateLinkService.UnrecognizedDestinationMessage,
            "Não reconhecemos este domínio. Use links de Shopee, TikTok Shop, Amazon, Mercado Livre, Magalu, Kabum! ou Casas Bahia.");
    }
}
