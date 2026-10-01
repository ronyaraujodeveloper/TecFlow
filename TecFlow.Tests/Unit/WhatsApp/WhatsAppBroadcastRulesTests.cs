using TecFlow.Business.Service.WhatsApp;
using TecFlow.Core.Entities;

namespace TecFlow.Tests.Unit.WhatsApp;

public class WhatsAppBroadcastRulesTests
{
    [Theory]
    [InlineData(5, 15)]
    [InlineData(15, 15)]
    [InlineData(30, 30)]
    [InlineData(500, 180)]
    public void ClampIntervalSeconds_ShouldKeepAntiBanWindow(int input, int expected)
    {
        Assert.Equal(expected, WhatsAppBroadcastRules.ClampIntervalSeconds(input));
    }

    [Fact]
    public void ApplyCommissionTag_ShouldReplacePlaceholder()
    {
        var text = WhatsAppBroadcastRules.ApplyCommissionTag(
            "Oferta [LINK_COMISSAO]",
            "https://shopee.com.br/x");

        Assert.Equal("Oferta https://shopee.com.br/x", text);
    }

    [Fact]
    public void SerializeJids_ShouldDeduplicate()
    {
        var json = WhatsAppBroadcastRules.SerializeJids(["  a@g.us ", "a@g.us", "b@g.us"]);
        var jids = WhatsAppBroadcastRules.DeserializeJids(json);

        Assert.Equal(2, jids.Count);
        Assert.Contains("a@g.us", jids);
        Assert.Contains("b@g.us", jids);
    }

    [Fact]
    public void ToUiStatus_ShouldMapCampaignStates()
    {
        Assert.Equal("Agendada", WhatsAppBroadcastRules.ToUiStatus(WhatsAppBroadcastStatuses.Pending));
        Assert.Equal("Enviando", WhatsAppBroadcastRules.ToUiStatus(WhatsAppBroadcastStatuses.Processing));
        Assert.Equal("Concluída", WhatsAppBroadcastRules.ToUiStatus(WhatsAppBroadcastStatuses.Completed));
        Assert.Equal("Falhou", WhatsAppBroadcastRules.ToUiStatus(WhatsAppBroadcastStatuses.Failed));
    }
}
