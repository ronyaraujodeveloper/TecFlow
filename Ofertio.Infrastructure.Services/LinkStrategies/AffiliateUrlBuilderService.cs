using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ofertio.Business.Integrations.MercadoLivre;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.LinkStrategies;
using Ofertio.Business.Service.Radar;
using Ofertio.Core.Enums;
using Ofertio.Database;

namespace Ofertio.Infrastructure.Services.LinkStrategies;

public sealed class AffiliateUrlBuilderService : IAffiliateUrlBuilderService
{
    private readonly AppDbContext _context;
    private readonly IOptionsMonitor<MercadoLivreIntegrationOptions> _mercadoLivreOptions;
    private readonly ILogger<AffiliateUrlBuilderService> _logger;

    public AffiliateUrlBuilderService(
        AppDbContext context,
        IOptionsMonitor<MercadoLivreIntegrationOptions> mercadoLivreOptions,
        ILogger<AffiliateUrlBuilderService> logger)
    {
        _context = context;
        _mercadoLivreOptions = mercadoLivreOptions;
        _logger = logger;
    }

    public async Task<string?> ResolveMercadoLivreMattToolAsync(int userId, CancellationToken cancellationToken = default)
    {
        var configured = _mercadoLivreOptions.CurrentValue.DefaultAffiliateTrackingId?.Trim();
        var fallback = string.IsNullOrWhiteSpace(configured)
            ? MercadoLivreIntegrationOptions.OfficialAffiliateTrackingId
            : configured;
        try
        {
            var tenantId = await _context.UserAccounts
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(user => user.Id == userId)
                .Select(user => (Guid?)user.TenantId)
                .FirstOrDefaultAsync(cancellationToken);
            var accounts = await _context.MarketplaceAccounts
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(x => x.IsActive
                    && (tenantId == null || x.TenantId == tenantId)
                    && (x.MarketplaceType == MarketplaceType.MercadoLivre
                        || x.FriendlyName != null
                        || x.ShopName != null))
                .Select(x => new
                {
                    x.MarketplaceType,
                    x.FriendlyName,
                    x.ShopName,
                    x.UserId,
                    x.TrackingId,
                    x.AffiliateTrackingId
                })
                .ToListAsync(cancellationToken);

            var userKey = userId.ToString(CultureInfo.InvariantCulture);
            var mattTool = accounts
                .Where(x => OfficialCatalogSearchRules.MatchesMercadoLivreAccount(x.MarketplaceType, x.FriendlyName, x.ShopName))
                .Select(x => new
                {
                    Owns = string.IsNullOrWhiteSpace(x.UserId) || string.Equals(x.UserId.Trim(), userKey, StringComparison.Ordinal),
                    Id = OfficialCatalogSearchRules.ResolveMercadoLivreAffiliateId(x.TrackingId, x.AffiliateTrackingId)
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Id))
                .OrderByDescending(x => x.Owns)
                .Select(x => x.Id)
                .FirstOrDefault();

            return string.IsNullOrWhiteSpace(mattTool) ? NullIfEmpty(fallback) : mattTool;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao resolver o matt_tool do Mercado Livre. UserId={UserId}", userId);
            return NullIfEmpty(fallback);
        }
    }

    public string BuildAffiliateUrl(
        string? originalUrl,
        string? primaryProductUrl,
        MarketplaceType? platform,
        string? mercadoLivreMattTool) =>
        AffiliateUrlBuilderRules.Build(originalUrl, primaryProductUrl, platform, mercadoLivreMattTool);

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
