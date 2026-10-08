using TecFlow.Infrastructure.Services.Radar;

namespace TecFlow.API.Workers;

public sealed class GlobalTrendingDealsWorker : BackgroundService
{
    private readonly GlobalTrendingDealsHost _host;

    public GlobalTrendingDealsWorker(GlobalTrendingDealsHost host)
    {
        _host = host;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        _host.RunForeverAsync(stoppingToken);
}
