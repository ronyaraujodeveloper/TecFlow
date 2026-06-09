using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.API.Controllers;

/// <summary>Redirect público do encurtador interno TecFlow (/r/{shortCode}).</summary>
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

    /// <summary>Resolve código curto, registra telemetria e redireciona ao marketplace.</summary>
    [HttpGet("/r/{shortCode}")]
    public async Task<IActionResult> RedirectAsync(string shortCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(shortCode) || shortCode.Length is < 6 or > 8)
        {
            return NotFound();
        }

        var normalizedCode = shortCode.Trim().ToLowerInvariant();
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
