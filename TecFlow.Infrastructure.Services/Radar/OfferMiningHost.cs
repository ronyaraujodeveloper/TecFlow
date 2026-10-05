using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Radar;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Radar;

public sealed class OfferMiningHost
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OfferMiningHost> _logger;

    public OfferMiningHost(IServiceScopeFactory scopeFactory, ILogger<OfferMiningHost> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task RunForeverAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await MineAllAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ciclo do worker de mineração falhou.");
            }

            try
            {
                await Task.Delay(AffiliateMiningRules.MiningInterval, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task MineAllAsync(CancellationToken cancellationToken)
    {
        List<int> userIds;
        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var accountIds = await context.MarketplaceAccounts
                .AsNoTracking()
                .Where(item => item.IsActive && item.UserId != null && item.UserId != "")
                .Select(item => item.UserId!)
                .Distinct()
                .ToListAsync(cancellationToken);
            var profileIds = await context.AffiliateMiningProfiles
                .AsNoTracking()
                .Select(item => item.UserId.ToString())
                .ToListAsync(cancellationToken);
            userIds = accountIds.Concat(profileIds)
                .Select(value => int.TryParse(value, out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();
        }

        foreach (var userId in userIds)
        {
            using var scope = _scopeFactory.CreateScope();
            var engine = scope.ServiceProvider.GetRequiredService<IOfferMiningEngine>();
            try
            {
                await engine.MineUserAsync(userId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Mineração falhou. UserId={UserId}", userId);
            }
        }
    }
}
