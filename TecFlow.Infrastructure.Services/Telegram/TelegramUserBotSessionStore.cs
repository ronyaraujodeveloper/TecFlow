using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TecFlow.Business.Integrations.Telegram;
using TecFlow.Business.Service.Telegram;

namespace TecFlow.Infrastructure.Services.Telegram;

public sealed class TelegramUserBotSessionStore
{
    private readonly TelegramBotOptions _options;
    private readonly ILogger<TelegramUserBotSessionStore> _logger;
    private readonly object _gate = new();
    private string? _resolvedFolder;

    public TelegramUserBotSessionStore(
        IOptions<TelegramBotOptions> options,
        ILogger<TelegramUserBotSessionStore> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string GetSessionPath(int userId) =>
        Path.Combine(GetSessionDirectory(), TelegramUserMonitorRules.ResolveSessionFileName(userId));

    public bool HasSession(int userId)
    {
        try
        {
            return File.Exists(GetSessionPath(userId));
        }
        catch (Exception ex) when (IsAccessFailure(ex))
        {
            _logger.LogWarning(ex, "Não foi possível consultar a sessão UserBot. UserId={UserId}", userId);
            return false;
        }
    }

    public string GetSessionDirectory()
    {
        lock (_gate)
        {
            if (!string.IsNullOrWhiteSpace(_resolvedFolder))
            {
                return _resolvedFolder;
            }

            _resolvedFolder = EnsureWritableDirectory();
            return _resolvedFolder;
        }
    }

    private string EnsureWritableDirectory()
    {
        var preferred = ResolvePreferredPath();
        if (TryCreateDirectory(preferred))
        {
            return preferred;
        }

        var fallback = Path.Combine(Path.GetTempPath(), "TecFlow", "telegram-sessions");
        _logger.LogError(
            "Sem permissão de escrita na pasta de sessões: {Path}. Redirecionando para TEMP {Fallback}",
            preferred,
            fallback);
        try
        {
            Directory.CreateDirectory(fallback);
            return fallback;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao criar pasta temporária de sessões: {Path}", fallback);
            throw;
        }
    }

    private string ResolvePreferredPath()
    {
        if (!string.IsNullOrWhiteSpace(_options.UserBotSessionFolder)
            && Path.IsPathRooted(_options.UserBotSessionFolder))
        {
            return _options.UserBotSessionFolder;
        }

        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(baseDir, "App_Data", "telegram-sessions");
    }

    private bool TryCreateDirectory(string sessionsPath)
    {
        try
        {
            if (!Directory.Exists(sessionsPath))
            {
                Directory.CreateDirectory(sessionsPath);
            }

            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Sem permissão de escrita na pasta de sessões: {Path}", sessionsPath);
            return false;
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Sem permissão de escrita na pasta de sessões: {Path}", sessionsPath);
            return false;
        }
    }

    private static bool IsAccessFailure(Exception ex) =>
        ex is UnauthorizedAccessException or IOException;
}
