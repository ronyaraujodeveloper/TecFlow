using TecFlow.Infrastructure.Services.Radar;

namespace TecFlow.API.Workers;

public sealed class DataPurgeWorker : BackgroundService
{
    private readonly DataPurgeHost _host;

    public DataPurgeWorker(DataPurgeHost host)
    {
        _host = host;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        _host.RunForeverAsync(stoppingToken);
}
