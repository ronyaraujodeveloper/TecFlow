using TecFlow.Infrastructure.Services.Radar;

namespace TecFlow.Tests.Unit.Radar;

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
        Assert.Equal("Frete grátis", item.Shipping);
    }
}
