using TecFlow.Infrastructure.Services.Telegram;

namespace TecFlow.API.Workers;

public sealed class TelegramUserMonitorWorker : BackgroundService
{
    private readonly TelegramUserMonitorHost _host;
    private readonly ILogger<TelegramUserMonitorWorker> _logger;

    public TelegramUserMonitorWorker(TelegramUserMonitorHost host, ILogger<TelegramUserMonitorWorker> logger)
    {
        _host = host;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _host.ReconcileAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ciclo do UserBot Telegram falhou.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
