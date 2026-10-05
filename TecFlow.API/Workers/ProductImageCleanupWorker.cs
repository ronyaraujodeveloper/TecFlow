using TecFlow.Infrastructure.Services.Groups;

namespace TecFlow.API.Workers;

public sealed class ProductImageCleanupWorker : BackgroundService
{
    private readonly ProductImageCleanupHost _host;

    public ProductImageCleanupWorker(ProductImageCleanupHost host)
    {
        _host = host;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        _host.RunForeverAsync(stoppingToken);
}
