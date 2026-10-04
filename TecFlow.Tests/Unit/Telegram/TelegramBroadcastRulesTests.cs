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
    public void ResolveChatIds_ShouldPreferJsonAndFallbackToLegacy()
    {
        var fromJson = TelegramBroadcastRules.ResolveChatIds("""["-1001","-1002"]""", "-1009");
        Assert.Equal(["-1001", "-1002"], fromJson);

        var legacy = TelegramBroadcastRules.ResolveChatIds("[]", "-100707440297");
        Assert.Equal(["-100707440297"], legacy);
    }

    [Fact]
    public void ToUiStatus_ShouldMapPending()
    {
        Assert.Equal("Agendada", TelegramBroadcastRules.ToUiStatus(TelegramBroadcastStatuses.Pending));
        Assert.Equal("Concluída", TelegramBroadcastRules.ToUiStatus(TelegramBroadcastStatuses.Completed));
    }
}
