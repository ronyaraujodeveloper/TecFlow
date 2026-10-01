using TecFlow.Business.Service.Telegram;
using TecFlow.Core.Entities;

namespace TecFlow.Tests.Unit.Telegram;

public class TelegramBroadcastRulesTests
{
    [Fact]
    public void ApplyCommissionTag_ShouldReplacePlaceholder()
    {
        var text = TelegramBroadcastRules.ApplyCommissionTag(
            "Oferta [LINK_COMISSAO]",
            "https://tecflow.local/x");
        Assert.Equal("Oferta https://tecflow.local/x", text);
    }

    [Fact]
    public void ToUiStatus_ShouldMapPending()
    {
        Assert.Equal("Agendada", TelegramBroadcastRules.ToUiStatus(TelegramBroadcastStatuses.Pending));
        Assert.Equal("Concluída", TelegramBroadcastRules.ToUiStatus(TelegramBroadcastStatuses.Completed));
    }
}
