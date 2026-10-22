using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ofertio.Business.Interfaces.Repositories;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Radar;

namespace Ofertio.API.Controllers;

/// <summary>Redirect público do encurtador interno Ofertio ({storeSlug}/{code}).</summary>
[ApiController]
[AllowAnonymous]
public class ShortLinkRedirectController : ControllerBase
{
    private readonly IShortAffiliateLinkRepository _shortLinkRepository;
    private readonly ILinkClickTelemetryService _telemetryService;
    private readonly ILogger<ShortLinkRedirectController> _logger;

    public ShortLinkRedirectController(
        IShortAffiliateLinkRepository shortLinkRepository,
        ILinkClickTelemetryService telemetryService,
        ILogger<ShortLinkRedirectController> logger)
    {
        _shortLinkRepository = shortLinkRepository;
        _telemetryService = telemetryService;
        _logger = logger;
    }

    /// <summary>Compatível com links antigos /r/{code}.</summary>
    [HttpGet("/r/{code}")]
    public Task<IActionResult> RedirectLegacyAsync(string code, CancellationToken cancellationToken) =>
        RedirectByCodeAsync(null, code, cancellationToken);

    /// <summary>Resolve o código curto com await e devolve o 302 sem esperar a gravação do clique.</summary>
    /// <remarks>O slug vem de <c>SlugHelper.GenerateSlug</c> e pode começar com dígito (ex.: apelido "97303724").</remarks>
    [HttpGet(StoreRedirectTemplate)]
    public Task<IActionResult> RedirectAsync(string storeSlug, string code, CancellationToken cancellationToken) =>
        RedirectByCodeAsync(storeSlug, code, cancellationToken);

    public const string StoreRedirectTemplate = "/{storeSlug:regex(^[[A-Za-z0-9]]{{1,80}}$)}/{code:length(6,8)}";

    private async Task<IActionResult> RedirectByCodeAsync(
        string? storeSlug,
        string code,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Short link recebido: slug={StoreSlug} code={ShortCode}",
            storeSlug ?? "(r)",
            code);

        if (string.IsNullOrWhiteSpace(code) || code.Length is < 6 or > 8)
        {
            return NotFound();
        }

        var normalizedCode = code.Trim().ToLowerInvariant();
        var link = await _shortLinkRepository.GetByShortCodeAsync(normalizedCode, cancellationToken);

        if (link is null)
        {
            _logger.LogInformation("Código curto não encontrado: {ShortCode}", normalizedCode);
            return NotFound();
        }

        _logger.LogInformation(
            "Short link {ShortCode} resolvido: redirect 302 para {DestinationHost}",
            normalizedCode,
            Uri.TryCreate(link.DestinationUrl, UriKind.Absolute, out var destination) ? destination.Host : "(url inválida)");

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();
        var referrer = Request.Headers.Referer.ToString();
        var pairs = Request.Query.Select(item =>
            new KeyValuePair<string, string>(item.Key, item.Value.ToString()));
        var tracking = OfferAttributionRules.ReadTracking(OfferAttributionRules.HttpRequestQuery.FromPairs(pairs));

        _telemetryService.EnqueueClickLog(
            link.AffiliateLinkId,
            ipAddress,
            userAgent,
            referrer,
            tracking.SourceChannel,
            tracking.SourceGroup,
            tracking.SubId);

        return Redirect(link.DestinationUrl);
    }
}
