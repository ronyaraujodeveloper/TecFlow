using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Radar;

public sealed class OfferIntelligenceEngine : IOfferIntelligenceEngine
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OfferIntelligenceEngine> _logger;

    public OfferIntelligenceEngine(
        IServiceScopeFactory scopeFactory,
        ILogger<OfferIntelligenceEngine> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task RunCycleAsync(CancellationToken cancellationToken = default)
    {
        List<int> userIds;
        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var fromCampaigns = await context.WhatsAppBroadcastCampaigns
                .AsNoTracking()
                .Select(item => item.UserId)
                .ToListAsync(cancellationToken);
            var fromTelegram = await context.TelegramBroadcastCampaigns
                .AsNoTracking()
                .Select(item => item.UserId)
                .ToListAsync(cancellationToken);
            var fromProfiles = await context.AffiliateMiningProfiles
                .AsNoTracking()
                .Select(item => item.UserId)
                .ToListAsync(cancellationToken);
            userIds = fromCampaigns.Concat(fromTelegram).Concat(fromProfiles).Distinct().ToList();
        }

        foreach (var userId in userIds)
        {
            using var scope = _scopeFactory.CreateScope();
            var health = scope.ServiceProvider.GetRequiredService<IOfferHealthService>();
            var evergreen = scope.ServiceProvider.GetRequiredService<IEvergreenLibraryService>();
            try
            {
                await health.InspectRecentCampaignsAsync(userId, cancellationToken);
                await evergreen.RefreshAsync(userId, cancellationToken);
                await evergreen.RecycleAsync(userId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ciclo de inteligência falhou. UserId={UserId}", userId);
            }
        }
    }
}

public sealed class OfferIntelligenceHost
{
    private readonly IOfferIntelligenceEngine _engine;
    private readonly ILogger<OfferIntelligenceHost> _logger;

    public OfferIntelligenceHost(IOfferIntelligenceEngine engine, ILogger<OfferIntelligenceHost> logger)
    {
        _engine = engine;
        _logger = logger;
    }

    public async Task RunForeverAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _engine.RunCycleAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Worker de saúde/evergreen falhou.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(12), cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
