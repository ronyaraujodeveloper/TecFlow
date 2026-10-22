using Ofertio.Business.Integrations.MercadoLivre;

namespace Ofertio.Tests.Unit.LinkStrategies;

public class MercadoLivreShowcaseRulesTests
{
    [Theory]
    [InlineData("https://www.mercadolivre.com.br/social/achadosdojoao?item_id=MLB4012345678", true)]
    [InlineData("https://www.mercadolivre.com.br/p/MLB19700000#reco_client=home_affiliate-profile", true)]
    [InlineData("https://www.mercadolivre.com.br/affiliate-profile/fulano", true)]
    [InlineData("https://produto.mercadolivre.com.br/MLB-123456789-fone-_JM", false)]
    [InlineData("https://shopee.com.br/social/loja", false)]
    public void IsAffiliateShowcaseUrl_ShouldDetectMeliSocialAndAffiliateProfile(string url, bool expected)
    {
        Assert.Equal(expected, MercadoLivreShowcaseRules.IsAffiliateShowcaseUrl(url));
    }

    [Theory]
    [InlineData(
        "https://www.mercadolivre.com.br/social/achados?item_id=MLB4012345678&matt_tool=1",
        "https://produto.mercadolivre.com.br/MLB-4012345678")]
    [InlineData(
        "https://www.mercadolivre.com.br/social/mastertechjr?matt_word=mastertechjr&matt_tool=88888888&item_id=MLB-4012345678&forceInApp=true",
        "https://produto.mercadolivre.com.br/MLB-4012345678")]
    [InlineData(
        "https://www.mercadolivre.com.br/social/achados?product_id=MLB19700000",
        "https://produto.mercadolivre.com.br/MLB-19700000")]
    [InlineData(
        "https://www.mercadolivre.com.br/p/MLB19700000?matt_tool=1&reco_client=home_affiliate-profile",
        "https://produto.mercadolivre.com.br/MLB-19700000")]
    [InlineData(
        "https://produto.mercadolivre.com.br/MLB-4012345678-fone-_JM?reco_client=home_affiliate-profile",
        "https://produto.mercadolivre.com.br/MLB-4012345678-fone-_JM")]
    [InlineData(
        "https://www.mercadolivre.com.br/social/achados#item_id=MLB-4012345678",
        "https://produto.mercadolivre.com.br/MLB-4012345678")]
    public void TryExtractFocusedProductUrl_ShouldReturnDirectProductPage(string url, string expected)
    {
        Assert.True(MercadoLivreShowcaseRules.TryExtractFocusedProductUrl(url, out var productUrl));
        Assert.Equal(expected, productUrl);
    }

    [Fact]
    public void TryExtractFocusedProductUrl_ShouldFail_WhenShowcaseHasNoFocusedProduct()
    {
        Assert.False(MercadoLivreShowcaseRules.TryExtractFocusedProductUrl(
            "https://www.mercadolivre.com.br/social/loja-oficial?matt_tool=1",
            out _));
    }

    [Theory]
    [InlineData(
        "https://produto.mercadolivre.com.br/MLB-4012345678-fone-_JM?matt_tool=55555555&tracking_id=abc",
        "https://produto.mercadolivre.com.br/MLB-4012345678-fone-_JM")]
    [InlineData(
        "https://www.mercadolivre.com.br/social/achados?item_id=MLB4012345678",
        "https://produto.mercadolivre.com.br/MLB-4012345678")]
    public void TryResolveDirectProductUrl_ShouldReturnCleanProductPage(string url, string expected)
    {
        Assert.True(MercadoLivreShowcaseRules.TryResolveDirectProductUrl(url, out var productUrl));
        Assert.Equal(expected, productUrl);
    }

    [Theory]
    [InlineData("https://meli.la/abc123")]
    [InlineData("https://www.mercadolivre.com.br/social/loja-oficial")]
    [InlineData("https://lista.mercadolivre.com.br/fone-bluetooth")]
    [InlineData("https://shopee.com.br/produto-i.1.2")]
    public void TryResolveDirectProductUrl_ShouldFail_ForNonProductPages(string url)
    {
        Assert.False(MercadoLivreShowcaseRules.TryResolveDirectProductUrl(url, out _));
    }

    [Fact]
    public void RemoveThirdPartyTracking_ShouldDropMattParameters()
    {
        var url = MercadoLivreShowcaseRules.RemoveThirdPartyTracking(
            "https://www.mercadolivre.com.br/p/MLB19700000?matt_tool_id=1&matt_tool=2&matt_d2id=3&matt_tracing_id=4&matt_word=5&color=azul");

        Assert.Equal("https://www.mercadolivre.com.br/p/MLB19700000?color=azul", url);
    }
}
