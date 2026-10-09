using TecFlow.Business.Service.Radar;

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
    }
}
