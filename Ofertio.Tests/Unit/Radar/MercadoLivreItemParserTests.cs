using Ofertio.Core.Entities;
using Ofertio.Infrastructure.Services.Radar;

namespace Ofertio.Tests.Unit.Radar;

public class MercadoLivreItemParserTests
{
    [Fact]
    public void ParseSearch_ShouldMapPermalinkPriceAndTitle()
    {
        const string json = """
            {"results":[{"id":"MLB123","title":"Notebook Gamer","price":1999.9,"permalink":"https://produto.mercadolivre.com.br/MLB-123","thumbnail":"https://http2.mlstatic.com/x.jpg","shipping":{"free_shipping":true}}]}
            """;
        var item = Assert.Single(MercadoLivreItemParser.ParseSearch(json));
        Assert.Equal("MLB123", item.ProductId);
        Assert.Equal("Notebook Gamer", item.ProductName);
        Assert.Equal(1999.9m, item.Price);
        Assert.Equal("https://produto.mercadolivre.com.br/MLB-123", item.SourceUrl);
        Assert.Equal("https://http2.mlstatic.com/x.jpg", item.ImageUrl);
        Assert.Equal("Frete grátis", item.Shipping);
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
