using TecFlow.Infrastructure.Services.Groups;

namespace TecFlow.API.Workers;

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
