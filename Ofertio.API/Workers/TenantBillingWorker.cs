using Ofertio.Business.Interfaces.Services;

namespace Ofertio.API.Workers;

/// <summary>A cada hora aplica bloqueio automático de inadimplentes e expiração de trials.</summary>
public sealed class TenantBillingWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TenantBillingWorker> _logger;

    public TenantBillingWorker(IServiceScopeFactory scopeFactory, ILogger<TenantBillingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var tenants = scope.ServiceProvider.GetRequiredService<ITenantAdminService>();
                var changed = await tenants.RunBillingCycleAsync(stoppingToken);
                if (changed > 0)
                {
                    _logger.LogInformation("Ciclo de cobrança SaaS alterou {Count} inquilino(s).", changed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha no ciclo de cobrança SaaS.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
