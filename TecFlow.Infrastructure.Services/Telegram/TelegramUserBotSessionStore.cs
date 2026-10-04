using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TecFlow.Business.Integrations.Telegram;
using TecFlow.Business.Service.Telegram;

namespace TecFlow.Infrastructure.Services.Telegram;

public sealed class TelegramUserBotSessionStore
{
    private readonly TelegramBotOptions _options;
    private readonly IHostEnvironment _environment;

    public TelegramUserBotSessionStore(IOptions<TelegramBotOptions> options, IHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;
    }

    public string GetSessionPath(int userId)
    {
        var folder = Path.Combine(
            _environment.ContentRootPath,
            string.IsNullOrWhiteSpace(_options.UserBotSessionFolder)
                ? "App_Data/telegram-sessions"
                : _options.UserBotSessionFolder);
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, TelegramUserMonitorRules.ResolveSessionFileName(userId));
    }

    public bool HasSession(int userId) => File.Exists(GetSessionPath(userId));
}
