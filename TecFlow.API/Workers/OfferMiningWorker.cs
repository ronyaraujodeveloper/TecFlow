using TecFlow.Infrastructure.Services.Radar;

namespace TecFlow.API.Workers;

public sealed class OfferMiningWorker : BackgroundService
{
    private readonly OfferMiningHost _host;

    public OfferMiningWorker(OfferMiningHost host)
    {
        _host = host;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        _host.RunForeverAsync(stoppingToken);
}
