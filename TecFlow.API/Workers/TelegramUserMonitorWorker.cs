using TecFlow.Infrastructure.Services.Telegram;

namespace TecFlow.API.Workers;

public sealed class TelegramUserMonitorWorker : BackgroundService
{
    private readonly TelegramUserMonitorHost _host;

    public TelegramUserMonitorWorker(TelegramUserMonitorHost host)
    {
        _host = host;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        _host.RunForeverAsync(stoppingToken);
}
