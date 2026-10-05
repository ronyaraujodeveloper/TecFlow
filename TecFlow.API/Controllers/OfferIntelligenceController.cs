using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Infrastructure.Interfaces;

namespace TecFlow.API.Controllers;

[Authorize]
[ApiController]
[Route("api/inteligencia")]
public sealed class OfferIntelligenceController : ControllerBase
{
    private readonly IOfferHealthService _health;
    private readonly IGroupAttributionService _attribution;
    private readonly IEvergreenLibraryService _evergreen;
    private readonly IOfferMediaStudio _media;
    private readonly IUserContextProvider _userContext;

    public OfferIntelligenceController(
        IOfferHealthService health,
        IGroupAttributionService attribution,
        IEvergreenLibraryService evergreen,
        IOfferMediaStudio media,
        IUserContextProvider userContext)
    {
        _health = health;
        _attribution = attribution;
        _evergreen = evergreen;
        _media = media;
        _userContext = userContext;
    }

    [HttpGet("saude")]
    public async Task<ActionResult<OfferHealthAlertResponseDto>> HealthAsync(CancellationToken cancellationToken) =>
        Ok(await _health.ListAsync(ResolveUserId(), cancellationToken));

    [HttpGet("atribuicao")]
    public async Task<ActionResult<GroupAttributionResponseDto>> AttributionAsync(CancellationToken cancellationToken) =>
        Ok(await _attribution.ListAsync(ResolveUserId(), cancellationToken));

    [HttpGet("evergreen")]
    public async Task<ActionResult<EvergreenLibraryResponseDto>> EvergreenAsync(CancellationToken cancellationToken) =>
        Ok(await _evergreen.ListAsync(ResolveUserId(), cancellationToken));

    [HttpPost("evergreen/atualizar")]
    public async Task<ActionResult<EvergreenLibraryResponseDto>> RefreshEvergreenAsync(CancellationToken cancellationToken) =>
        Ok(await _evergreen.RefreshAsync(ResolveUserId(), cancellationToken));

    [HttpPost("evergreen/reciclar")]
    public async Task<ActionResult<EvergreenLibraryResponseDto>> RecycleAsync(CancellationToken cancellationToken) =>
        Ok(await _evergreen.RecycleAsync(ResolveUserId(), cancellationToken));

    [HttpPost("midia/moldura")]
    public async Task<ActionResult<OfferMediaResponseDto>> FrameAsync(
        [FromBody] OfferMediaRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await _media.ApplyFrameAsync(ResolveUserId(), request ?? new OfferMediaRequestDto(), cancellationToken));

    [HttpPost("midia/video")]
    public async Task<ActionResult<OfferMediaResponseDto>> VideoAsync(
        [FromBody] OfferMediaRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await _media.ExtractVideoAsync(request ?? new OfferMediaRequestDto(), cancellationToken));

    private int ResolveUserId()
    {
        var fromContext = _userContext.GetCurrentUserId();
        if (fromContext is > 0)
        {
            return fromContext.Value;
        }

        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrWhiteSpace(claim) && int.TryParse(claim, out var userId) && userId > 0
            ? userId
            : 1;
    }
}
