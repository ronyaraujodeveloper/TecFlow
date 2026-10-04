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
            TryMigrateLegacySessions(_resolvedFolder);
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

        var common = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        if (string.IsNullOrWhiteSpace(common))
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "telegram-sessions");
        }

        if (!string.IsNullOrWhiteSpace(_options.UserBotSessionFolder))
        {
            var relative = _options.UserBotSessionFolder
                .Replace('/', Path.DirectorySeparatorChar)
                .TrimStart(Path.DirectorySeparatorChar);
            return Path.Combine(common, "TecFlow", relative);
        }

        return Path.Combine(common, "TecFlow", "telegram-sessions");
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

    private void TryMigrateLegacySessions(string destination)
    {
        try
        {
            var legacy = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "telegram-sessions");
            if (!Directory.Exists(legacy) || string.Equals(legacy, destination, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(legacy, "user-*.session"))
            {
                var target = Path.Combine(destination, Path.GetFileName(file));
                if (!File.Exists(target))
                {
                    File.Copy(file, target, overwrite: false);
                }
            }
        }
        catch (Exception ex) when (IsAccessFailure(ex))
        {
            _logger.LogWarning(ex, "Não foi possível migrar sessões UserBot antigas de App_Data.");
        }
    }

    private static bool IsAccessFailure(Exception ex) =>
        ex is UnauthorizedAccessException or IOException;
}
