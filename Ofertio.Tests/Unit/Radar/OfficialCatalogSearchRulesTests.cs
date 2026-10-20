using Ofertio.Business.Dto;
using Ofertio.Business.Service.Radar;
using Ofertio.Core.Enums;
using Ofertio.Database.Filter;

namespace Ofertio.Tests.Unit.Radar;

public class OfficialCatalogSearchRulesTests
{
    [Fact]
    public void BuildShorterQueries_ShouldDropLastWordsProgressively()
    {
        Assert.Equal(
            ["Dell Inspiron i7", "Dell Inspiron", "Dell"],
            OfficialCatalogSearchRules.BuildShorterQueries("  Dell  Inspiron i7 16GB "));
        Assert.Empty(OfficialCatalogSearchRules.BuildShorterQueries("notebook"));
        Assert.Empty(OfficialCatalogSearchRules.BuildShorterQueries("a b"));
    }

    [Fact]
    public void ApplySort_ShouldOrderByLowestPriceOrBiggestDiscountAndKeepRelevance()
    {
        IReadOnlyList<OfficialCatalogProductDto> items =
        [
            new() { ProductId = "A", Price = 300m, OriginalPrice = 330m },
            new() { ProductId = "B", Price = 100m, OriginalPrice = 200m },
            new() { ProductId = "C", Price = null },
            new() { ProductId = "D", Price = 200m }
        ];

        Assert.Equal(["B", "D", "A", "C"], OfficialCatalogSearchRules.ApplySort(items, "menor_preco").Select(item => item.ProductId));
        Assert.Equal(["B", "A", "D", "C"], OfficialCatalogSearchRules.ApplySort(items, "maior_desconto").Select(item => item.ProductId));
        Assert.Equal(["A", "B", "C", "D"], OfficialCatalogSearchRules.ApplySort(items, "relevancia").Select(item => item.ProductId));
        Assert.Equal(["A", "B", "C", "D"], OfficialCatalogSearchRules.ApplySort(items, null).Select(item => item.ProductId));
    }

    [Fact]
    public void PassesOptionalFilters_FreeShippingOnly_ShouldRequireFreeShipping()
    {
        var filter = new OfficialCatalogSearchFilter { FreeShippingOnly = true };

        Assert.True(OfficialCatalogSearchRules.PassesOptionalFilters(new OfficialCatalogProductDto { Shipping = "Frete grátis" }, filter));
        Assert.False(OfficialCatalogSearchRules.PassesOptionalFilters(new OfficialCatalogProductDto { Shipping = "R$ 19,90" }, filter));
        Assert.False(OfficialCatalogSearchRules.PassesOptionalFilters(new OfficialCatalogProductDto(), filter));
        Assert.True(OfficialCatalogSearchRules.PassesOptionalFilters(new OfficialCatalogProductDto(), new OfficialCatalogSearchFilter()));
    }

    [Fact]
    public void HumanizeMercadoLivreDomain_ShouldTurnDomainIdIntoReadableName()
    {
        Assert.Equal("Gaming headsets", OfficialCatalogSearchRules.HumanizeMercadoLivreDomain("MLB-GAMING_HEADSETS"));
        Assert.Equal("Notebooks", OfficialCatalogSearchRules.HumanizeMercadoLivreDomain("MLB-NOTEBOOKS"));
    }

    [Fact]
    public void DescribeMercadoLivreScan_ShouldShowOffersScannedAndShortenedTerm()
    {
        Assert.Equal("12 ofertas · 80 produtos analisados", OfficialCatalogSearchRules.DescribeMercadoLivreScan(12, 80, "dell", "Dell"));
        Assert.Equal("1 oferta · 20 produtos analisados", OfficialCatalogSearchRules.DescribeMercadoLivreScan(1, 20, null, "dell"));
        Assert.Equal(
            "0 ofertas · 100 produtos analisados (sem anúncio ativo)",
            OfficialCatalogSearchRules.DescribeMercadoLivreScan(0, 100, "dell i7", "dell i7"));
        Assert.Equal(
            "3 ofertas · 45 produtos analisados · buscado como \"dell inspiron\"",
            OfficialCatalogSearchRules.DescribeMercadoLivreScan(3, 45, "dell inspiron", "dell inspiron i7 16gb"));
    }

    [Fact]
    public void IsValidQuery_ShouldRequireTwoCharacters()
    {
        Assert.False(OfficialCatalogSearchRules.IsValidQuery(" "));
        Assert.False(OfficialCatalogSearchRules.IsValidQuery("a"));
        Assert.True(OfficialCatalogSearchRules.IsValidQuery("nb"));
    }

    [Fact]
    public void HasRealAffiliateCredentials_ShouldRejectSandboxPlaceholders()
    {
        Assert.False(OfficialCatalogSearchRules.HasRealAffiliateCredentials("100000", "tecflow-homolog-shopee-sandbox"));
        Assert.True(OfficialCatalogSearchRules.HasRealAffiliateCredentials("123456", "prod-secret"));
    }

    [Fact]
    public void CountVisibleOffers_ShouldApplyFiltersAndIgnoreDuplicateUrls()
    {
        OfficialCatalogProductDto Offer(string url, decimal price) => new() { SourceUrl = url, Price = price };
        var items = new[]
        {
            Offer("https://produto.mercadolivre.com.br/MLB-1?matt_tool=14343296", 100),
            Offer("https://produto.mercadolivre.com.br/MLB-1?matt_tool=14343296", 100),
            Offer("https://produto.mercadolivre.com.br/MLB-2?matt_tool=14343296", 900)
        };

        Assert.Equal(2, OfficialCatalogSearchRules.CountVisibleOffers(items, new OfficialCatalogSearchFilter()));
        Assert.Equal(1, OfficialCatalogSearchRules.CountVisibleOffers(items, new OfficialCatalogSearchFilter { MaxPrice = 500 }));
    }

    [Theory]
    [InlineData(System.Net.HttpStatusCode.Forbidden, "", true)]
    [InlineData(System.Net.HttpStatusCode.Unauthorized, "", true)]
    [InlineData(System.Net.HttpStatusCode.BadRequest, "{\"error\":\"unauthorized_scopes\"}", true)]
    [InlineData(System.Net.HttpStatusCode.InternalServerError, "{\"error\":\"internal\"}", false)]
    [InlineData(System.Net.HttpStatusCode.OK, "{\"results\":[]}", false)]
    public void IsMercadoLivreSearchRefused_ShouldDetectForbiddenAndScopeErrors(System.Net.HttpStatusCode status, string body, bool expected)
    {
        Assert.Equal(expected, OfficialCatalogSearchRules.IsMercadoLivreSearchRefused(status, body));
    }

    [Fact]
    public void EscapeGraphQl_ShouldEscapeQuotes()
    {
        Assert.Equal("fone \\\"bt\\\"", OfficialCatalogSearchRules.EscapeGraphQl("fone \"bt\""));
        Assert.Equal(20, OfficialCatalogSearchRules.ClampLimit(99));
    }

    [Fact]
    public void BuildChannelStatus_ShouldMapOkEmptyAndMissingCredentials()
    {
        var ok = OfficialCatalogSearchRules.BuildChannelStatus(
            "MercadoLivre", "Mercado Livre", true, 15, false, string.Empty);
        Assert.Equal("ok", ok.State);
        Assert.Equal("15 produtos", ok.Message);

        var empty = OfficialCatalogSearchRules.BuildChannelStatus(
            "MercadoLivre", "Mercado Livre", true, 0, false, string.Empty);
        Assert.Equal("empty", empty.State);
        Assert.Equal("0 produtos", empty.Message);

        var shopee = OfficialCatalogSearchRules.BuildChannelStatus(
            "Shopee",
            "Shopee",
            true,
            0,
            true,
            OfficialCatalogSearchRules.ShopeeMissingApiKeyMessage);
        Assert.Equal("missing", shopee.State);
        Assert.Equal("Chave de API não configurada", shopee.Message);

        var amazon = OfficialCatalogSearchRules.BuildChannelStatus(
            "Amazon",
            "Amazon",
            true,
            0,
            true,
            OfficialCatalogSearchRules.AmazonMissingPaApiMessage);
        Assert.Equal("PA-API não configurada", amazon.Message);

        var mlMissing = OfficialCatalogSearchRules.BuildChannelStatus(
            "MercadoLivre",
            "Mercado Livre",
            true,
            0,
            true,
            OfficialCatalogSearchRules.MercadoLivreMissingAccountMessage);
        Assert.Equal("missing", mlMissing.State);
        Assert.Equal("Requer OAuth do Mercado Livre para buscar ofertas", mlMissing.Message);

        var error = OfficialCatalogSearchRules.BuildChannelStatus(
            "MercadoLivre",
            "Mercado Livre",
            true,
            0,
            false,
            string.Empty,
            "Erro HTTP 403 (Forbidden)");
        Assert.Equal("error", error.State);
        Assert.Equal("Erro HTTP 403 (Forbidden)", error.Message);

        var recovered = OfficialCatalogSearchRules.BuildChannelStatus(
            "MercadoLivre",
            "Mercado Livre",
            true,
            12,
            false,
            string.Empty,
            "ML HTTP 403 (Forbidden)");
        Assert.Equal("ok", recovered.State);
        Assert.Equal("12 produtos", recovered.Message);
    }

    [Fact]
    public void MatchesMercadoLivreAccount_ShouldIgnoreSpacesInStoreName()
    {
        Assert.True(OfficialCatalogSearchRules.MatchesMercadoLivreAccount(
            MarketplaceType.MercadoLivre, "Loja", null));
        Assert.True(OfficialCatalogSearchRules.IsMercadoLivreLabel("Mercado Livre"));
        Assert.True(OfficialCatalogSearchRules.HasConnectedMercadoLivreStore(
            MarketplaceType.Shopee,
            "Mercado Livre",
            null,
            "14343296",
            null));
        Assert.Equal(
            "14343296",
            OfficialCatalogSearchRules.ResolveMercadoLivreAffiliateId(null, "14343296"));
    }

    [Fact]
    public void PassesOptionalFilters_ShouldNotRequireKeywordInTitle()
    {
        var item = new OfficialCatalogProductDto
        {
            ProductName = "Ultrabook Gamer",
            Price = 10,
            SourceUrl = "https://www.mercadolivre.com.br/p/MLB1"
        };

        Assert.True(OfficialCatalogSearchRules.PassesOptionalFilters(
            item,
            new OfficialCatalogSearchFilter { Keyword = "notebook 16GB" }));
        Assert.False(OfficialCatalogSearchRules.PassesOptionalFilters(
            item,
            new OfficialCatalogSearchFilter { MinPrice = 50 }));
    }
}
