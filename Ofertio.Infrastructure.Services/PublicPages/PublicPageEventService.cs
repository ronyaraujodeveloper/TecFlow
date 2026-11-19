using Microsoft.Extensions.Logging;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Attribution;
using Ofertio.Business.Service.Geo;
using Ofertio.Business.Service.LinkStrategies;
using Ofertio.Core.Entities;
using Ofertio.Database;

namespace Ofertio.Infrastructure.Services.PublicPages;

public sealed class PublicPageEventService : IPublicPageEventService
{
    private readonly AppDbContext _context;
    private readonly IGeoLocationService? _geo;
    private readonly ILocationCatalog? _locations;
    private readonly ILogger<PublicPageEventService> _logger;

    public PublicPageEventService(
        AppDbContext context,
        ILogger<PublicPageEventService> logger,
        IGeoLocationService? geo = null,
        ILocationCatalog? locations = null)
    {
        _context = context;
        _logger = logger;
        _geo = geo;
        _locations = locations;
    }

    public async Task RecordAsync(
        Guid tenantId,
        int userId,
        string kind,
        PublicVisitorInfo visitor,
        int? joinLinkId = null,
        string? channel = null,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty || userId <= 0)
        {
            return;
        }

        try
        {
            // A contagem nunca atrapalha o visitante: qualquer falha aqui é só registrada.
            var geo = _geo?.Lookup(visitor.IpAddress);
            var place = _locations is null || geo is null
                ? LocationIds.Empty
                : await _locations.ResolveAsync(geo, cancellationToken);
            var userAgent = string.IsNullOrWhiteSpace(visitor.UserAgent) ? null : LinkClickTelemetryHelper.Truncate(visitor.UserAgent, 512);
            await _context.PublicPageEvents.AddAsync(new PublicPageEvent
            {
                TenantId = tenantId,
                UserId = userId,
                Kind = kind,
                JoinLinkId = joinLinkId,
                Channel = channel,
                IsBot = ClickClassifierRules.IsBot(visitor.UserAgent),
                IpMasked = LinkClickTelemetryHelper.MaskIpAddress(visitor.IpAddress),
                UserAgent = userAgent,
                DeviceType = LinkClickTelemetryHelper.DetectDeviceType(visitor.UserAgent),
                CountryId = place.CountryId,
                StateId = place.StateId,
                CityId = place.CityId
            }, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Não foi possível registrar o evento {Kind} da página pública.", kind);
        }
    }
}
