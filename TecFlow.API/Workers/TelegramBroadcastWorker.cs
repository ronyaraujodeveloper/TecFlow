using TecFlow.Business.Interfaces.Services;

namespace TecFlow.API.Workers;

public sealed class TelegramBroadcastWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TelegramBroadcastWorker> _logger;

    public TelegramBroadcastWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<TelegramBroadcastWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var broadcasts = scope.ServiceProvider.GetRequiredService<ITelegramBroadcastService>();
                await broadcasts.ProcessDueCampaignsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ciclo do disparo Telegram falhou.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
