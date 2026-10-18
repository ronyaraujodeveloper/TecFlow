using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ofertio.Business.Integrations.Telegram;
using Ofertio.Infrastructure.Services.Telegram;

namespace Ofertio.Tests.Unit.Telegram;

public class TelegramUserBotSessionStoreTests
{
    [Fact]
    public void GetSessionDirectory_ShouldExistAndContainTelegramSessions()
    {
        var store = new TelegramUserBotSessionStore(
            Options.Create(new TelegramBotOptions()),
            NullLogger<TelegramUserBotSessionStore>.Instance);

        var directory = store.GetSessionDirectory();

        Assert.True(Directory.Exists(directory));
        Assert.Contains("telegram-sessions", directory, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("inetpub", directory, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(
            Path.Combine(directory, "user-7.session"),
            store.GetSessionPath(7));
    }
}
