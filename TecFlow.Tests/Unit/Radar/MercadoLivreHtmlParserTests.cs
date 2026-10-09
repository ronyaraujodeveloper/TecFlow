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
