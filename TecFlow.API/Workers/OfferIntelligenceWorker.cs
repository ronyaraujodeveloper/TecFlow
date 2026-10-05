using TecFlow.Infrastructure.Services.Radar;

namespace TecFlow.API.Workers;

public sealed class OfferIntelligenceWorker : BackgroundService
{
    private readonly OfferIntelligenceHost _host;

    public OfferIntelligenceWorker(OfferIntelligenceHost host)
    {
        _host = host;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        _host.RunForeverAsync(stoppingToken);
}
