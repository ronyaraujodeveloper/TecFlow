using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.API.Controllers;

/// <summary>Redirect público do encurtador interno TecFlow ({storeSlug}/{code}).</summary>
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
        RedirectByCodeAsync(code, cancellationToken);

    /// <summary>Resolve código curto, registra telemetria e redireciona ao marketplace.</summary>
    [HttpGet("/{storeSlug:regex(^[[A-Za-z]][[A-Za-z0-9]]{{0,79}}$)}/{code:length(6,8)}")]
    public Task<IActionResult> RedirectAsync(string storeSlug, string code, CancellationToken cancellationToken) =>
        RedirectByCodeAsync(code, cancellationToken);

    private async Task<IActionResult> RedirectByCodeAsync(string code, CancellationToken cancellationToken)
    {
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

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();
        var referrer = Request.Headers.Referer.ToString();

        _telemetryService.EnqueueClickLog(link.AffiliateLinkId, ipAddress, userAgent, referrer);

        return Redirect(link.DestinationUrl);
    }
}
