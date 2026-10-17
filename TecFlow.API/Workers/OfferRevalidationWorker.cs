using TecFlow.Infrastructure.Services.Groups;

namespace TecFlow.API.Workers;

public sealed class OfferRevalidationWorker : BackgroundService
{
    private readonly OfferRevalidationHost _host;

    public OfferRevalidationWorker(OfferRevalidationHost host)
    {
        _host = host;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        _host.RunForeverAsync(stoppingToken);
}
