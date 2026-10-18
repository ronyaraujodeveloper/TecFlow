using TecFlow.Business.Integrations.MercadoLivre;
using TecFlow.Core.Entities;

namespace TecFlow.Tests.Unit.LinkStrategies;

public class MercadoLivreAvailabilityRulesTests
{
    private const string ProductUrl = "https://www.mercadolivre.com.br/p/MLB19700000";

    [Theory]
    [InlineData("<div>Anúncio pausado</div>", GroupOfferStatuses.Inativo)]
    [InlineData("<div>Publicação finalizada</div><h2>Produtos relacionados</h2>", GroupOfferStatuses.Inativo)]
    [InlineData("<div>Sem estoque</div>", GroupOfferStatuses.Esgotado)]
    [InlineData("<h2>Produtos relacionados</h2>", GroupOfferStatuses.Esgotado)]
    [InlineData("{\"availability\":\"https://schema.org/OutOfStock\"}", GroupOfferStatuses.Esgotado)]
    public void DetectUnavailableStatus_ShouldFlagPagesWithoutDirectPurchase(string html, string expected)
    {
        Assert.Equal(expected, MercadoLivreAvailabilityRules.DetectUnavailableStatus(html, ProductUrl, ProductUrl));
    }

    [Theory]
    [InlineData("<button>Comprar agora</button><h2>Produtos relacionados</h2>")]
    [InlineData("<button>Adicionar ao carrinho</button><span>Tamanho G: Sem estoque</span>")]
    [InlineData("<html>Verificando se você é humano</html>")]
    [InlineData("")]
    public void DetectUnavailableStatus_ShouldKeepPurchasablePagesAndUnknownContent(string html)
    {
        Assert.Null(MercadoLivreAvailabilityRules.DetectUnavailableStatus(html, ProductUrl, ProductUrl));
    }

    [Fact]
    public void DetectUnavailableStatus_ShouldFlagProductRedirectedToSearch()
    {
        var status = MercadoLivreAvailabilityRules.DetectUnavailableStatus(
            "<html>Resultados</html>",
            ProductUrl,
            "https://lista.mercadolivre.com.br/fone-bluetooth");

        Assert.Equal(GroupOfferStatuses.Esgotado, status);
    }

    [Fact]
    public void IsRedirectedToSearch_ShouldIgnoreLinksThatWereAlreadySearches()
    {
        const string search = "https://lista.mercadolivre.com.br/fone-bluetooth";

        Assert.False(MercadoLivreAvailabilityRules.IsRedirectedToSearch(search, search));
        Assert.False(MercadoLivreAvailabilityRules.IsRedirectedToSearch(ProductUrl, ProductUrl));
    }

    [Theory]
    [InlineData("paused", 5, GroupOfferStatuses.Inativo)]
    [InlineData("closed", null, GroupOfferStatuses.Inativo)]
    [InlineData("active", 0, GroupOfferStatuses.Esgotado)]
    [InlineData("active", 3, null)]
    [InlineData("active", null, null)]
    public void FromApiItem_ShouldMapStatusAndStock(string status, int? quantity, string? expected)
    {
        Assert.Equal(expected, MercadoLivreAvailabilityRules.FromApiItem(status, quantity));
    }

    [Theory]
    [InlineData(ProductUrl, true)]
    [InlineData("https://produto.mercadolivre.com.br/MLB-4012345678-fone-_JM", true)]
    [InlineData("https://shopee.com.br/produto-i.1.2", false)]
    public void IsMercadoLivreUrl_ShouldRecognizeHosts(string url, bool expected)
    {
        Assert.Equal(expected, MercadoLivreAvailabilityRules.IsMercadoLivreUrl(url));
    }
}
