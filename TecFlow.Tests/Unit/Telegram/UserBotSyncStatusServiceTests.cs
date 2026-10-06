using TecFlow.Infrastructure.Services.Telegram;

namespace TecFlow.Tests.Unit.Telegram;

public class UserBotSyncStatusServiceTests
{
    [Fact]
    public void MarkFailed_ShouldExposeExactMessageForBlazor()
    {
        var service = new UserBotSyncStatusService();
        service.MarkFailed(1, "Acesso negado à pasta de sessões");
        var status = service.Get(1);
        Assert.True(status.IsFailed);
        Assert.Contains("Acesso negado à pasta de sessões", status.Message, StringComparison.Ordinal);
        Assert.StartsWith("Falha na captura:", status.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Get_WithoutUserEntry_ShouldReturnGlobalError()
    {
        var service = new UserBotSyncStatusService();
        service.MarkFailed(0, "WTelegramClient recusou a sessão");
        var status = service.Get(7);
        Assert.True(status.IsFailed);
        Assert.Contains("WTelegramClient recusou a sessão", status.Message, StringComparison.Ordinal);
    }
}
