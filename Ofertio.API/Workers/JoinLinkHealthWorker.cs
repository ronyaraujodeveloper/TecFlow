using Microsoft.Extensions.Options;
using Ofertio.Infrastructure.Services.PublicPages;

namespace Ofertio.API.Workers;

/// <summary>
/// A cada 6 horas: confere os grupos dos botões da página pública (só os que estão há mais de 20 h sem conferência,
/// então na prática uma vez por dia) e apaga o IP mascarado vencido.
/// </summary>
public sealed class JoinLinkHealthWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);
    private static readonly TimeSpan PauseBetweenCalls = TimeSpan.FromSeconds(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<JoinLinkHealthWorker> _logger;

    public JoinLinkHealthWorker(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<JoinLinkHealthWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var health = scope.ServiceProvider.GetRequiredService<JoinLinkHealthService>();
                var changed = await health.RunAsync(PauseBetweenCalls, stoppingToken);
                if (changed > 0)
                {
                    _logger.LogInformation("Conferência dos grupos desativou {Count} botão(ões) da página pública.", changed);
                }

                var days = _configuration.GetValue("Privacy:IpRetentionDays", IpRetentionService.DefaultDays);
                var purged = await scope.ServiceProvider.GetRequiredService<IpRetentionService>().PurgeAsync(days, stoppingToken);
                if (purged > 0)
                {
                    _logger.LogInformation("Retenção de IP: {Count} registro(s) limpos (prazo {Days} dias).", purged, days);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha na conferência dos grupos ou na retenção de IP.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
