using Ofertio.Infrastructure.Services.Groups;

namespace Ofertio.API.Workers;

public sealed class ProductTagBackfillWorker : BackgroundService
{
    private readonly ProductTagBackfillHost _host;

    public ProductTagBackfillWorker(ProductTagBackfillHost host)
    {
        _host = host;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        _host.RunOnceAsync(stoppingToken);
}
