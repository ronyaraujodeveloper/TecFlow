using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Groups;
using TecFlow.Core.Entities;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Groups;

public sealed class OfferRevalidationService : IOfferRevalidationService
{
    private readonly AppDbContext _context;
    private readonly ILiveCheckSearchService _liveCheck;
    private readonly ILogger<OfferRevalidationService> _logger;

    public OfferRevalidationService(
        AppDbContext context,
        ILiveCheckSearchService liveCheck,
        ILogger<OfferRevalidationService> logger)
    {
        _context = context;
        _liveCheck = liveCheck;
        _logger = logger;
    }

    public async Task<OfferRevalidationSummaryDto> RevalidateActiveOffersAsync(
        int batchSize,
        TimeSpan minimumAge,
        TimeSpan delayBetweenChecks,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var dueBefore = now - minimumAge;
        var recentCutoff = now.Subtract(ProductImageStorageRules.Retention);
        var offers = await _context.GroupCapturedMessages
            .Where(item => item.IsAvailable
                && !item.IsIgnored
                && item.CreatedAt >= recentCutoff
                && (item.OfferStatus == GroupOfferStatuses.Ativo
                    || item.OfferStatus == GroupOfferStatuses.PrecoAlterado
                    || item.OfferStatus == GroupOfferStatuses.Verificando)
                && (item.LastValidatedAt == null || item.LastValidatedAt <= dueBefore))
            .OrderBy(item => item.LastValidatedAt ?? DateTime.MinValue)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        var summary = new OfferRevalidationSummaryDto();
        foreach (var offer in offers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await _liveCheck.CheckAndPersistAsync(offer, offer.UserId, cancellationToken);
                summary.Checked++;
                if (!offer.IsAvailable || GroupOfferStatuses.IsInactive(offer.OfferStatus))
                {
                    summary.Deactivated++;
                    _logger.LogInformation(
                        "Oferta desativada na revalidação. OfferId={OfferId} UserId={UserId} Status={Status}",
                        offer.Id,
                        offer.UserId,
                        offer.OfferStatus);
                }
                else if (offer.OfferStatus == GroupOfferStatuses.PrecoSuspeito)
                {
                    summary.Suspicious++;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                summary.Failed++;
                _context.Entry(offer).State = EntityState.Detached;
                _logger.LogWarning(ex, "Falha ao revalidar oferta. OfferId={OfferId}", offer.Id);
            }

            if (delayBetweenChecks > TimeSpan.Zero)
            {
                await Task.Delay(delayBetweenChecks, cancellationToken);
            }
        }

        _logger.LogInformation(
            "Revalidação de ofertas concluída. Verificadas={Checked} Desativadas={Deactivated} PrecoSuspeito={Suspicious} Falhas={Failed}",
            summary.Checked,
            summary.Deactivated,
            summary.Suspicious,
            summary.Failed);
        return summary;
    }
}

public sealed class OfferRevalidationHost
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<OfferRevalidationOptions> _options;
    private readonly ILogger<OfferRevalidationHost> _logger;

    public OfferRevalidationHost(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<OfferRevalidationOptions> options,
        ILogger<OfferRevalidationHost> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    public async Task RunForeverAsync(CancellationToken cancellationToken)
    {
        if (!await DelayAsync(_options.CurrentValue.InitialDelay, cancellationToken))
        {
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            var options = _options.CurrentValue;
            if (options.Enabled)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var revalidation = scope.ServiceProvider.GetRequiredService<IOfferRevalidationService>();
                    await revalidation.RevalidateActiveOffersAsync(
                        options.EffectiveBatchSize,
                        options.Interval,
                        options.DelayBetweenChecks,
                        cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Worker de revalidação de ofertas falhou.");
                }
            }

            if (!await DelayAsync(options.Interval, cancellationToken))
            {
                break;
            }
        }
    }

    private static async Task<bool> DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }
}
