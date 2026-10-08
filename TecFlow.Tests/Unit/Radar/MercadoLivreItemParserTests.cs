using TecFlow.Infrastructure.Services.Radar;

namespace TecFlow.Tests.Unit.Radar;

public class MercadoLivreItemParserTests
{
    [Fact]
    public void Parse_ShouldReadPriceStatusAndShipping()
    {
        const string json = """
            {"id":"MLB123","title":"Cadeira Gamer","price":199.9,"original_price":299.9,"status":"active","shipping":{"free_shipping":true},"secure_thumbnail":"https://img.ml/a.jpg"}
            """;

        var snapshot = MercadoLivreItemParser.Parse(json);
        Assert.NotNull(snapshot);
        Assert.True(snapshot!.IsAvailable);
        Assert.Equal(199.9m, snapshot.Price);
        Assert.Equal(299.9m, snapshot.OriginalPrice);
        Assert.Equal("Cadeira Gamer", snapshot.ProductName);
        Assert.Contains("Frete grátis", snapshot.Shipping, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Api", snapshot.Source);
    }

    [Fact]
    public void Parse_ShouldMarkClosedAsUnavailable()
    {
        var snapshot = MercadoLivreItemParser.Parse("""{"id":"MLB1","title":"X","price":10,"status":"closed"}""");
        Assert.NotNull(snapshot);
        Assert.False(snapshot!.IsAvailable);
    }
}
