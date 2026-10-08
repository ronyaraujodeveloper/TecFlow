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
}
