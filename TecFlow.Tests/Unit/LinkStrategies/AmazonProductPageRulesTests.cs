using TecFlow.Business.Integrations.Amazon;
using TecFlow.Business.Service.LinkStrategies;

namespace TecFlow.Tests.Unit.LinkStrategies;

public class AmazonProductPageRulesTests
{
    private const string UnitPriceBlock = """
        <span class="a-size-mini a-color-base aok-align-center pricePerUnit">(<span class="a-price a-text-price" data-a-size="mini" data-a-color="base"><span class="a-offscreen">R$&nbsp;16,60</span><span aria-hidden="true">R$&nbsp;16,60</span></span> / unidade)</span>
        """;

    private const string StrikePrice = """
        <span class="a-price a-text-price" data-a-size="s" data-a-strike="true" data-a-color="secondary"><span class="a-offscreen">R$&nbsp;79,90</span><span aria-hidden="true">R$&nbsp;79,90</span></span>
        """;

    [Fact]
    public void TryExtractBuyBoxPrice_ShouldReadPriceToPay_IgnoringUnitAndStrikePrice()
    {
        var html = $"""
            <html><body>
            <div id="corePriceDisplay_desktop_feature_div">
              {UnitPriceBlock}
              <span class="a-price aok-align-center reinventPricePriceToPayMargin priceToPay"><span class="a-offscreen">R$&nbsp;55,00</span>
                <span aria-hidden="true"><span class="a-price-symbol">R$</span><span class="a-price-whole">55<span class="a-price-decimal">,</span></span><span class="a-price-fraction">00</span></span></span>
              De: {StrikePrice}
            </div>
            </body></html>
            """;

        Assert.Equal(55.00m, AmazonProductPageRules.TryExtractBuyBoxPrice(html));
    }

    [Fact]
    public void TryExtractBuyBoxPrice_ShouldSkipUnitPriceInsideCorePriceContainer()
    {
        var html = $"""
            <div id="corePrice_feature_div">
              {StrikePrice}
              {UnitPriceBlock}
              <span class="a-price" data-a-size="xl"><span class="a-offscreen">R$&nbsp;1.299,00</span></span>
            </div>
            """;

        Assert.Equal(1299.00m, AmazonProductPageRules.TryExtractBuyBoxPrice(html));
    }

    [Fact]
    public void TryExtractBuyBoxPrice_ShouldReadApexWholeAndFraction_WhenOffscreenIsMissing()
    {
        const string html = """
            <div id="apex_desktop"><span class="a-price a-text-price a-size-medium apexPriceToPay" data-a-size="b">
            <span aria-hidden="true"><span class="a-price-whole">2.349<span class="a-price-decimal">,</span></span><span class="a-price-fraction">90</span></span></span></div>
            """;

        Assert.Equal(2349.90m, AmazonProductPageRules.TryExtractBuyBoxPrice(html));
    }

    [Fact]
    public void TryExtractBuyBoxPrice_ShouldFallbackToOpenGraphPrice()
    {
        const string html = """<html><head><meta property="og:price:amount" content="55.00" /></head><body>R$ 16,60 / unidade</body></html>""";

        Assert.Equal(55.00m, AmazonProductPageRules.TryExtractBuyBoxPrice(html));
    }

    [Fact]
    public void TryExtractBuyBoxPrice_ShouldReturnNull_WhenPageHasNoBuyBox()
    {
        Assert.Null(AmazonProductPageRules.TryExtractBuyBoxPrice("<html><title>Amazon.com.br</title><body>Digite os caracteres</body></html>"));
    }

    [Fact]
    public void GenericParser_ShouldNotTakeUnitPriceAsProductPrice()
    {
        var html = $"<html><body>{UnitPriceBlock}<span class=\"a-size-mini\">(R$ 16,60 / unidade)</span><p>Preço: R$ 55,00</p></body></html>";

        var metadata = ProductMetadataHtmlParser.Parse(html, "https://www.amazon.com.br/dp/B0TESTE123");

        Assert.Equal(55.00m, metadata.ProductPrice);
    }

    [Theory]
    [InlineData("https://www.amazon.com.br/dp/B0TESTE123", true)]
    [InlineData("https://amzn.to/abc123", true)]
    [InlineData("https://shopee.com.br/produto-i.1.2", false)]
    public void IsAmazonUrl_ShouldRecognizeAmazonHosts(string url, bool expected)
    {
        Assert.Equal(expected, AmazonProductPageRules.IsAmazonUrl(url));
    }
}
