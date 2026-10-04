using System.Collections.Concurrent;

namespace TecFlow.Infrastructure.Services.Telegram;

public sealed class TelegramUserBotCodeStore
{
    private readonly ConcurrentDictionary<int, string> _codes = new();

    public void Set(int userId, string code)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(code))
        {
            return;
        }

        _codes[userId] = code.Trim();
    }

    public string? Wait(int userId, TimeSpan timeout)
    {
        var until = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < until)
        {
            if (_codes.TryRemove(userId, out var code) && !string.IsNullOrWhiteSpace(code))
            {
                return code;
            }

            Thread.Sleep(400);
        }

        return null;
    }
}
