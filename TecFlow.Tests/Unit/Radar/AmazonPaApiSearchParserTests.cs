using TecFlow.Infrastructure.Services.Radar;

namespace TecFlow.Tests.Unit.Radar;

public class AmazonPaApiSearchParserTests
{
    [Fact]
    public void ParseSearch_ShouldMapAsinAndDetailUrl()
    {
        const string json = """
            {"SearchResult":{"Items":[{"ASIN":"B00TEST","DetailPageURL":"https://www.amazon.com.br/dp/B00TEST","ItemInfo":{"Title":{"DisplayValue":"Echo Dot"}},"Offers":{"Listings":[{"Price":{"Amount":199.9}}]}}]}}
            """;
        var item = Assert.Single(AmazonPaApiService.ParseSearch(json));
        Assert.Equal("B00TEST", item.ProductId);
        Assert.Equal("Echo Dot", item.ProductName);
        Assert.Equal(199.9m, item.Price);
        Assert.Equal("https://www.amazon.com.br/dp/B00TEST", item.SourceUrl);
    }
}
