using TecFlow.Business.Service.Radar;

namespace TecFlow.Tests.Unit.Radar;

public class OfferAttributionRulesTests
{
    [Fact]
    public void BuildSubId_ShouldCombineChannelAndGroup()
    {
        var subId = OfferAttributionRules.BuildSubId("WhatsApp", "120363@g.us");
        Assert.Equal("WhatsApp_120363@g.us", subId);
    }

    [Fact]
    public void AppendTracking_ShouldInjectQueryOnShortLink()
    {
        var url = OfferAttributionRules.AppendTracking("http://localhost:5001/Loja/abc1234", "Telegram", "-1001");
        Assert.Contains("tf_src=Telegram", url, StringComparison.Ordinal);
        Assert.Contains("tf_grp=-1001", url, StringComparison.Ordinal);
        Assert.Contains("sub_id=Telegram_-1001", url, StringComparison.Ordinal);
    }

    [Fact]
    public void StampMessage_ShouldReplaceFirstUrlOnly()
    {
        var stamped = OfferAttributionRules.StampMessage(
            "Oferta\nhttps://shopee.com.br/item",
            "WhatsApp",
            "grupo1");

        Assert.Contains("tf_src=WhatsApp", stamped, StringComparison.Ordinal);
        Assert.StartsWith("Oferta", stamped, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadTracking_ShouldParseQueryPairs()
    {
        var query = OfferAttributionRules.HttpRequestQuery.FromPairs(
        [
            new("tf_src", "WhatsApp"),
            new("tf_grp", "g1"),
            new("sub_id", "WhatsApp_g1")
        ]);

        var tracking = OfferAttributionRules.ReadTracking(query);
        Assert.Equal("WhatsApp", tracking.SourceChannel);
        Assert.Equal("g1", tracking.SourceGroup);
        Assert.Equal("WhatsApp_g1", tracking.SubId);
    }
}
