using TecFlow.Infrastructure.Services.Radar;

namespace TecFlow.API.Workers;

public sealed class PreFlightWorker : BackgroundService
{
    private readonly PreFlightHost _host;

    public PreFlightWorker(PreFlightHost host)
    {
        _host = host;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        _host.RunForeverAsync(stoppingToken);
}
