using TecFlow.Business.Dto;
using TecFlow.Business.Service.Radar;
using TecFlow.Core.Enums;
using TecFlow.Database.Filter;

namespace TecFlow.Tests.Unit.Radar;

public class OfficialCatalogSearchRulesTests
{
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
        Assert.Equal("15 produtos encontrados", ok.Message);

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
        Assert.Equal("Requer conta conectada no painel", mlMissing.Message);

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
