using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ofertio.Business.Dto;
using Ofertio.Business.Integrations.MercadoLivre;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Radar;
using Ofertio.Core.Entities;
using Ofertio.Database;
using Ofertio.Database.Filter;

namespace Ofertio.Infrastructure.Services.Radar;

public sealed class OfferRadarHybridQuery : IOfferRadarHybridQuery
{
    private readonly AppDbContext _context;
    private readonly ICatalogOfferIndex _index;
    private readonly IOfficialCatalogSearchService _catalog;
    private readonly IOptionsMonitor<MercadoLivreIntegrationOptions> _options;
    private readonly ILogger<OfferRadarHybridQuery> _logger;

    public OfferRadarHybridQuery(
        AppDbContext context,
        ICatalogOfferIndex index,
        IOfficialCatalogSearchService catalog,
        IOptionsMonitor<MercadoLivreIntegrationOptions> options,
        ILogger<OfferRadarHybridQuery> logger)
    {
        _context = context;
        _index = index;
        _catalog = catalog;
        _options = options;
        _logger = logger;
    }

    public async Task<string?> RefreshProfileTermsAsync(
        int userId,
        AffiliateMiningProfileDto profile,
        int maxApiTerms,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return null;
        }

        var terms = ResolveTerms(profile);
        var cacheMinutes = _options.CurrentValue.ResolveSearchCacheMinutes();
        var now = DateTime.UtcNow;
        var apiCalls = 0;
        var cap = Math.Max(0, maxApiTerms);

        foreach (var term in terms)
        {
            if (apiCalls >= cap)
            {
                break;
            }

            try
            {
                var fresh = cacheMinutes > 0
                    && await _index.HasFreshTermAsync(userId, term, now.AddMinutes(-cacheMinutes), cancellationToken);
                if (fresh)
                {
                    _logger.LogDebug(
                        "Termo '{Term}' do perfil já está na base local dentro do cache de {Minutes} min. UserId={UserId}",
                        term,
                        cacheMinutes,
                        userId);
                    continue;
                }

                apiCalls++;
                _logger.LogInformation(
                    "Termo '{Term}' sem resultado recente na base local. Consultando as APIs oficiais. UserId={UserId} CacheMinutos={Minutes}",
                    term,
                    userId,
                    cacheMinutes);
                await _catalog.SearchAsync(
                    userId,
                    new OfficialCatalogSearchFilter
                    {
                        Keyword = term,
                        Limit = OfferIndexRules.MiningSearchLimit
                    },
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao consultar o termo '{Term}' nas APIs oficiais. UserId={UserId}", term, userId);
            }
        }

        try
        {
            await PromoteIndexedOffersAsync(userId, profile, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao promover ofertas indexadas para o radar. UserId={UserId}", userId);
        }

        return apiCalls switch
        {
            0 => null,
            1 => "1 termo do perfil foi consultado nas lojas oficiais porque não havia resultado recente na base local.",
            _ => $"{apiCalls} termos do perfil foram consultados nas lojas oficiais porque não havia resultado recente na base local."
        };
    }

    private async Task PromoteIndexedOffersAsync(
        int userId,
        AffiliateMiningProfileDto profile,
        CancellationToken cancellationToken)
    {
        var niches = ResolveTerms(profile);
        var since = DateTime.UtcNow.Subtract(AffiliateMiningRules.HistoryWindow);
        var indexed = await _context.IndexedCatalogOffers
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.IndexedAt >= since && item.Price > 0)
            .OrderByDescending(item => item.IndexedAt)
            .Take(400)
            .ToListAsync(cancellationToken);
        var dayAgo = DateTime.UtcNow.AddHours(-24);
        var known = await _context.OfferRadarItems
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.ReceivedAt >= dayAgo)
            .Select(item => item.OriginalUrl)
            .ToListAsync(cancellationToken);
        var knownUrls = known.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var created = 0;

        foreach (var offer in indexed)
        {
            if (created >= OfferIndexRules.PromotePerCycle)
            {
                break;
            }

            if (!OfferIndexRules.BelongsToProfile(offer.SearchTerm, offer.ProductName, offer.ProductModel, niches)
                || !AffiliateMiningRules.MatchesTicket(offer.Price, profile.MinTicket, profile.MaxTicket)
                || string.IsNullOrWhiteSpace(offer.SourceUrl)
                || !knownUrls.Add(offer.SourceUrl))
            {
                continue;
            }

            _context.OfferRadarItems.Add(new OfferRadarItem
            {
                UserId = userId,
                ProductName = offer.ProductName,
                ProductImageUrl = offer.ProductImageUrl,
                OriginalUrl = offer.SourceUrl,
                AffiliateUrl = offer.SourceUrl,
                PlatformType = offer.PlatformType,
                PlatformName = offer.PlatformName,
                Price = offer.Price,
                AttractivenessScore = AffiliateMiningRules.ComputeAttractivenessScore(
                    offer.Price,
                    null,
                    false,
                    true,
                    false,
                    false),
                Source = OfferRadarSources.Catalog,
                IsAutoQueued = profile.AutoPilotEnabled,
                ReceivedAt = offer.IndexedAt
            });
            created++;
        }

        if (created > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Ofertas da base local promovidas ao radar. UserId={UserId} Novas={Count}",
                userId,
                created);
        }
    }

    private static IReadOnlyList<string> ResolveTerms(AffiliateMiningProfileDto profile)
    {
        IEnumerable<string> source = profile.Niches is { Count: > 0 }
            ? profile.Niches
            : AffiliateMiningRules.DefaultNiches;
        return source
            .Select(OfferIndexRules.NormalizeTerm)
            .Where(OfficialCatalogSearchRules.IsValidQuery)
            .Distinct(StringComparer.Ordinal)
            .Take(OfferIndexRules.WorkerRefreshTermCap)
            .ToList();
    }
}
