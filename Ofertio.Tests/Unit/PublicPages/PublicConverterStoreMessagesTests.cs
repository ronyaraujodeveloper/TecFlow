using Ofertio.Business.Service.PublicPages;
using Ofertio.Core.Enums;
using Ofertio.SharedUi.Helpers;

namespace Ofertio.Tests.Unit.PublicPages;

public class PublicConverterStoreMessagesTests
{
    private static readonly MarketplaceType[] Accepted = [MarketplaceType.MercadoLivre, MarketplaceType.Shopee];

    [Theory]
    [InlineData(MarketplaceType.MercadoLivre, "do Mercado Livre")]
    [InlineData(MarketplaceType.Shopee, "da Shopee")]
    [InlineData(MarketplaceType.Amazon, "da Amazon")]
    [InlineData(MarketplaceType.TikTokShop, "do TikTok Shop")]
    [InlineData(MarketplaceType.Kabum, "do Kabum!")]
    [InlineData(MarketplaceType.CasasBahia, "da Casas Bahia")]
    public void StoreReference_ShouldUseTheRightArticle(MarketplaceType platform, string expected)
    {
        Assert.Equal(expected, PublicConverterStoreMessages.StoreReference(platform));
    }

    [Fact]
    public void NotServed_ShouldMatchTheApprovedSentence()
    {
        var message = PublicConverterStoreMessages.NotServed(MarketplaceType.Amazon, " Rony ");

        Assert.Equal("Esse link é da Amazon, que Rony ainda não atende. Use um link de uma das lojas abaixo.", message);
    }

    [Fact]
    public void NotServedSite_ShouldCiteTheSiteWithoutWwwAndFallBackWithoutName()
    {
        var message = PublicConverterStoreMessages.NotServedSite("https://www.netshoes.com.br/tenis-x", null);

        Assert.Equal(
            "Esse link é do site netshoes.com.br, que o afiliado desta página ainda não atende. Use um link de uma das lojas abaixo.",
            message);
    }

    [Theory]
    [InlineData("https://www.mercadolivre.com.br/produto/p/MLB123")]
    [InlineData("https://shopee.com.br/produto-i.1.2")]
    [InlineData("https://bit.ly/abc123")]
    [InlineData("bit.ly/abc123")]
    [InlineData("")]
    public void InputCheck_ShouldLetAcceptedStoresAndShortLinksThrough(string url)
    {
        Assert.Null(PublicConverterInputCheck.NotServedMessage(url, Accepted, "Rony"));
    }

    [Fact]
    public void InputCheck_ShouldRefuseAKnownStoreThatIsNotConnected()
    {
        var message = PublicConverterInputCheck.NotServedMessage("https://www.amazon.com.br/dp/B0TESTE", Accepted, "Rony");

        Assert.Equal("Esse link é da Amazon, que Rony ainda não atende. Use um link de uma das lojas abaixo.", message);
    }

    [Fact]
    public void InputCheck_ShouldRefuseAnUnknownSite()
    {
        var message = PublicConverterInputCheck.NotServedMessage("https://www.netshoes.com.br/tenis-corrida", Accepted, "Rony");

        Assert.Equal("Esse link é do site netshoes.com.br, que Rony ainda não atende. Use um link de uma das lojas abaixo.", message);
    }

    [Theory]
    [InlineData("https://www.netsho")]
    [InlineData("netshoes.com.br/tenis")]
    public void InputCheck_WhileTyping_ShouldWaitForACompleteAddress(string url)
    {
        Assert.Null(PublicConverterInputCheck.NotServedMessage(url, Accepted, "Rony", whileTyping: true));
    }

    [Fact]
    public void InputCheck_WithoutConnectedStores_ShouldLeaveItToTheServer()
    {
        Assert.Null(PublicConverterInputCheck.NotServedMessage("https://www.netshoes.com.br/x", [], "Rony"));
    }
}
