using TecFlow.Business.Integrations.MercadoLivre;
using TecFlow.Business.Service.Radar;

namespace TecFlow.Tests.Unit.Radar;

public class MercadoLivreHtmlParserTests
{
    [Fact]
    public void ParseSearchResults_ShouldReadPermalinkAndTitleFromHtml()
    {
        const string html = """
            <html>
            <a class="ui-search-link" title="Notebook Dell i7"
               href="https://www.mercadolivre.com.br/notebook-dell-i7/p/MLB1234567890">Dell</a>
            {"permalink":"https://produto.mercadolivre.com.br/MLB-9876543210","title":"Fone Bluetooth","price":99.9}
            </html>
            """;

        var items = MercadoLivreHtmlParser.ParseSearchResults(html);

        Assert.Contains(items, item => item.ProductId == "MLB1234567890" && item.ProductName == "Notebook Dell i7");
        Assert.Contains(items, item => item.SourceUrl.Contains("MLB-9876543210", StringComparison.OrdinalIgnoreCase));
        Assert.All(items, item => Assert.Equal("Web", item.Source));
    }

    [Fact]
    public void ParseSearchResults_ShouldReadEveryLayoutItemCard()
    {
        const string html = """
            <ol>
              <li class="ui-search-layout__item">
                <a href="/notebook-um/_JM"><h2>Notebook Um</h2></a>
                <span class="andes-money-amount__fraction">1.199</span>
                <img src="https://http2.mlstatic.com/um.jpg" />
              </li>
              <li class="ui-search-layout__item">
                <a href="https://www.mercadolivre.com.br/notebook-dois/p/MLB222"><h2>Notebook Dois</h2></a>
                <span class="andes-money-amount__fraction">2.299</span>
                <img data-src="https://http2.mlstatic.com/dois.jpg" />
              </li>
              <div class="ui-search-result__content">
                <a href="https://produto.mercadolivre.com.br/MLB-333" title="Notebook Três">x</a>
                <h2>Notebook Três</h2>
                <span class="andes-money-amount__fraction">399</span>
              </div>
            </ol>
            """;

        var items = MercadoLivreHtmlParser.ParseSearchResults(html);

        Assert.Equal(3, items.Count);
        Assert.Equal("Notebook Um", items[0].ProductName);
        Assert.Equal(1199m, items[0].Price);
        Assert.Contains("notebook-um", items[0].SourceUrl, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Notebook Dois", items[1].ProductName);
        Assert.Equal("MLB222", items[1].ProductId);
        Assert.Equal("Notebook Três", items[2].ProductName);
    }

    [Fact]
    public void InjectMattTool_ShouldAppendTrackingId()
    {
        var url = MercadoLivreCommissionUrlBuilder.InjectMattTool(
            "https://www.mercadolivre.com.br/p/MLB1234567890",
            "14343296");

        Assert.Contains("matt_tool=14343296", url, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildMercadoLivreListaUrl_ShouldSlugifyQuery()
    {
        Assert.Equal(
            "https://lista.mercadolivre.com.br/dell-i7",
            OfficialCatalogSearchRules.BuildMercadoLivreListaUrl("dell i7"));
    }
}
