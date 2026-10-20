using Ofertio.Core.Entities;
using Ofertio.Infrastructure.Services.Radar;

namespace Ofertio.Tests.Unit.Radar;

public class MercadoLivreItemParserTests
{
    [Fact]
    public void ParseCatalogSearchIds_ShouldKeepRelevanceOrderWithoutDuplicates()
    {
        const string json = """
            {"keywords":"dell i7","paging":{"total":3},"results":[{"id":"MLB200","name":"A"},{"id":"MLB100","name":"B"},{"id":"MLB200","name":"A"},{"name":"sem id"}]}
            """;

        Assert.Equal(["MLB200", "MLB100"], MercadoLivreItemParser.ParseCatalogSearchIds(json));
        Assert.Empty(MercadoLivreItemParser.ParseCatalogSearchIds("""{"message":"forbidden"}"""));
    }

    [Fact]
    public void ParseCatalogOffer_ShouldMapBuyBoxWinnerPriceAndProductPage()
    {
        const string json = """
            {"id":"MLB123","name":"Notebook Gamer","permalink":"https://www.mercadolivre.com.br/notebook-gamer/p/MLB123","pictures":[{"url":"http://http2.mlstatic.com/x.jpg"}],"buy_box_winner":{"item_id":"MLB999","price":1999.9,"original_price":2499,"shipping":{"free_shipping":true}}}
            """;

        var item = MercadoLivreItemParser.ParseCatalogOffer(json);

        Assert.NotNull(item);
        Assert.Equal("MLB123", item!.ProductId);
        Assert.Equal("Notebook Gamer", item.ProductName);
        Assert.Equal(1999.9m, item.Price);
        Assert.Equal(2499m, item.OriginalPrice);
        Assert.Equal("https://www.mercadolivre.com.br/notebook-gamer/p/MLB123", item.SourceUrl);
        Assert.Equal("https://http2.mlstatic.com/x.jpg", item.ImageUrl);
        Assert.Equal("Frete grátis", item.Shipping);
    }

    [Theory]
    [InlineData("""{"id":"MLB123","name":"Sem oferta","buy_box_winner":null}""")]
    [InlineData("""{"id":"MLB123","name":"Sem preço","buy_box_winner":{"item_id":"MLB9"}}""")]
    [InlineData("""{"message":"forbidden","error":"forbidden","status":403}""")]
    public void ParseCatalogOffer_WithoutActiveBuyBoxOffer_ShouldReturnNull(string json)
    {
        Assert.Null(MercadoLivreItemParser.ParseCatalogOffer(json));
    }

    [Fact]
    public void ParseCatalogOffer_WithoutWinner_ShouldUseMinimumOfPriceRange()
    {
        var item = MercadoLivreItemParser.ParseCatalogOffer(
            """{"id":"MLB7","name":"Notebook","buy_box_winner":null,"buy_box_winner_price_range":{"min":{"price":3500,"currency_id":"BRL"},"max":{"price":4200}}}""");

        Assert.Equal(3500m, item!.Price);
        Assert.Equal("https://www.mercadolivre.com.br/p/MLB7", item.SourceUrl);
    }

    [Fact]
    public void ParseCatalogItemsOffer_ShouldPickCheapestPricedItem()
    {
        var item = MercadoLivreItemParser.ParseCatalogItemsOffer(
            """{"id":"MLB7","name":"Notebook","pictures":[{"url":"https://http2.mlstatic.com/n.jpg"}]}""",
            """{"results":[{"item_id":"MLB71","price":4999},{"item_id":"MLB72"},{"item_id":"MLB73","price":4599.9,"original_price":5200,"shipping":{"free_shipping":true}}]}""");

        Assert.Equal(4599.9m, item!.Price);
        Assert.Equal(5200m, item.OriginalPrice);
        Assert.Equal("Notebook", item.ProductName);
        Assert.Equal("Frete grátis", item.Shipping);
        Assert.Null(MercadoLivreItemParser.ParseCatalogItemsOffer("""{"id":"MLB7"}""", """{"results":[]}"""));
    }

    [Fact]
    public void ParseCatalogChildrenIds_ShouldReturnChildrenOfParentProduct()
    {
        Assert.Equal(
            ["MLB31", "MLB32"],
            MercadoLivreItemParser.ParseCatalogChildrenIds("""{"id":"MLB30","children_ids":["MLB31","MLB32","MLB31"]}"""));
        Assert.Empty(MercadoLivreItemParser.ParseCatalogChildrenIds("""{"id":"MLB31","children_ids":[]}"""));
    }

    [Theory]
    [InlineData("""{"id":"MLB1","status":"active","available_quantity":0,"price":10}""", false, GroupOfferStatuses.Esgotado)]
    [InlineData("""{"id":"MLB1","status":"paused","available_quantity":4,"price":10}""", false, GroupOfferStatuses.Inativo)]
    [InlineData("""{"id":"MLB1","status":"active","available_quantity":4,"price":10}""", true, GroupOfferStatuses.Ativo)]
    public void Parse_ShouldMarkSoldOutAndPausedItemsUnavailable(string json, bool available, string status)
    {
        var item = MercadoLivreItemParser.Parse(json);

        Assert.NotNull(item);
        Assert.Equal(available, item.IsAvailable);
        Assert.Equal(status, item.Status);
    }
}
