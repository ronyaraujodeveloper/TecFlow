using Microsoft.AspNetCore.Hosting;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Telegram;
using TecFlow.Infrastructure.Services.Telegram;

namespace TecFlow.API.Workers;

public sealed class TelegramUserMonitorWorker : BackgroundService
{
    private readonly TelegramUserMonitorHost _host;
    private readonly TelegramUserBotSessionStore _sessions;
    private readonly IUserBotSyncStatusService _syncStatus;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<TelegramUserMonitorWorker> _logger;

    public TelegramUserMonitorWorker(
        TelegramUserMonitorHost host,
        TelegramUserBotSessionStore sessions,
        IUserBotSyncStatusService syncStatus,
        IServiceScopeFactory scopeFactory,
        IWebHostEnvironment environment,
        ILogger<TelegramUserMonitorWorker> logger)
    {
        _host = host;
        _sessions = sessions;
        _syncStatus = syncStatus;
        _scopeFactory = scopeFactory;
        _environment = environment;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var folders = UserBotRuntimeRules.EnsureLocalFolders(_environment.WebRootPath);
            _ = _sessions.GetSessionDirectory();
            var baseWebRoot = _environment.WebRootPath
                ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
            _logger.LogInformation(
                "Pastas do UserBot prontas. Uploads={Uploads} AppDataSessions={Sessions} WebRoot={WebRoot}",
                folders.UploadsPath,
                folders.SessionsPath,
                baseWebRoot);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var backfill = scope.ServiceProvider.GetRequiredService<IProductImageUrlLinkBackfillService>();
                var linked = await backfill.LinkExistingFilesOnceAsync(cancellationToken);
                if (linked > 0)
                {
                    _logger.LogInformation("ImageUrl vinculado a arquivos já existentes no disco. Count={Count}", linked);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Backfill único de ImageUrl a partir do disco falhou.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro crítico ao criar pastas do UserBot");
            _syncStatus.MarkFailed(0, ex.Message);
        }

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Iniciando conexão com WTelegramClient...");
            await _host.RunForeverAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro crítico na execução em segundo plano do UserBot");
            _syncStatus.MarkFailed(0, ex.Message);
        }
    }
}
