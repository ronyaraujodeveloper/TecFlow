using Ofertio.Infrastructure.Services.Groups;

namespace Ofertio.API.Workers;

public sealed class CouponReclassificationWorker : BackgroundService
{
    private readonly CouponReclassificationHost _host;

    public CouponReclassificationWorker(CouponReclassificationHost host)
    {
        _host = host;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        _host.RunOnceAsync(stoppingToken);
}
