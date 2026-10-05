using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Infrastructure.Interfaces;

namespace TecFlow.API.Controllers;

[Authorize]
[ApiController]
[Route("api/radar")]
public sealed class OfferRadarController : ControllerBase
{
    private const int LocalFallbackUserId = 1;

    private readonly IAffiliateMiningProfileService _profiles;
    private readonly IProductArbitrageService _arbitrage;
    private readonly IOfferRadarService _radar;
    private readonly IUserContextProvider _userContext;
    private readonly ILogger<OfferRadarController> _logger;

    public OfferRadarController(
        IAffiliateMiningProfileService profiles,
        IProductArbitrageService arbitrage,
        IOfferRadarService radar,
        IUserContextProvider userContext,
        ILogger<OfferRadarController> logger)
    {
        _profiles = profiles;
        _arbitrage = arbitrage;
        _radar = radar;
        _userContext = userContext;
        _logger = logger;
    }

    [HttpGet("perfil")]
    public async Task<ActionResult<AffiliateMiningProfileResponseDto>> GetProfileAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _profiles.GetAsync(ResolveUserId(), cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao carregar perfil de mineração.");
            return Ok(new AffiliateMiningProfileResponseDto { Status = false, Descricao = "Não foi possível carregar o perfil." });
        }
    }

    [HttpPut("perfil")]
    public async Task<ActionResult<AffiliateMiningProfileResponseDto>> SaveProfileAsync(
        [FromBody] AffiliateMiningProfileDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _profiles.SaveAsync(ResolveUserId(), request ?? new AffiliateMiningProfileDto(), cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao salvar perfil de mineração.");
            return Ok(new AffiliateMiningProfileResponseDto { Status = false, Descricao = "Não foi possível salvar o perfil." });
        }
    }

    [HttpPost("arbitragem")]
    public async Task<ActionResult<OfferArbitrageResponseDto>> SearchArbitrageAsync(
        [FromBody] OfferArbitrageRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _arbitrage.SearchAlternativesAsync(
                ResolveUserId(),
                request ?? new OfferArbitrageRequestDto(),
                cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha na busca de arbitragem.");
            return Ok(new OfferArbitrageResponseDto { Status = false, Descricao = "Não foi possível buscar preços menores." });
        }
    }

    [HttpGet("ofertas")]
    public async Task<ActionResult<OfferRadarResponseDto>> ListAsync(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _radar.ListAsync(ResolveUserId(), skip, take, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao listar o radar de ofertas.");
            return Ok(new OfferRadarResponseDto { Status = false, Descricao = "Não foi possível carregar o radar." });
        }
    }

    [HttpPost("ofertas/{id:int}/agendar")]
    public async Task<ActionResult<OfferRadarResponseDto>> ScheduleAsync(
        int id,
        [FromQuery] string? channel,
        CancellationToken cancellationToken)
    {
        return Ok(await _radar.ScheduleAsync(ResolveUserId(), id, channel, cancellationToken));
    }

    [HttpPost("ofertas/{id:int}/piloto")]
    public async Task<ActionResult<OfferRadarResponseDto>> AutoPilotAsync(
        int id,
        CancellationToken cancellationToken)
    {
        return Ok(await _radar.QueueAutoPilotAsync(ResolveUserId(), id, cancellationToken));
    }

    private int ResolveUserId()
    {
        var fromContext = _userContext.GetCurrentUserId();
        if (fromContext is > 0)
        {
            return fromContext.Value;
        }

        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(claim) && int.TryParse(claim, out var userId) && userId > 0)
        {
            return userId;
        }

        return LocalFallbackUserId;
    }
}
