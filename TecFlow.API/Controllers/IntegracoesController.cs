using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Database.Filter;

namespace TecFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/integracoes")]
public class IntegracoesController : ControllerBase
{
    private readonly IIntegracaoLojaService _integracaoLojaService;

    public IntegracoesController(IIntegracaoLojaService integracaoLojaService)
    {
        _integracaoLojaService = integracaoLojaService;
    }

    /// <summary>Lista lojas marketplace vinculadas ao usuário autenticado.</summary>
    [HttpGet("lojas")]
    public async Task<ActionResult<IntegracaoLojaResponseDto>> ListAsync(
        [FromQuery] IntegracaoLojaFilter filter,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new IntegracaoLojaResponseDto
            {
                Status = false,
                Descricao = "Usuário não autenticado."
            });
        }

        var result = await _integracaoLojaService.ListByUserAsync(userId.Value, filter, cancellationToken);
        return Ok(result);
    }

    /// <summary>Vincula nova loja marketplace via callback OAuth.</summary>
    [HttpPost("vincular")]
    public async Task<ActionResult<IntegracaoLojaResponseDto>> LinkAsync(
        [FromBody] IntegracaoLojaDto dto,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new IntegracaoLojaResponseDto
            {
                Status = false,
                Descricao = "Usuário não autenticado."
            });
        }

        var result = await _integracaoLojaService.LinkAsync(userId.Value, dto, cancellationToken);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    /// <summary>Remove/desvincula uma loja marketplace específica.</summary>
    [HttpDelete("lojas/{id:int}")]
    public async Task<ActionResult<IntegracaoLojaResponseDto>> UnlinkAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new IntegracaoLojaResponseDto
            {
                Status = false,
                Descricao = "Usuário não autenticado."
            });
        }

        var result = await _integracaoLojaService.UnlinkAsync(userId.Value, id, cancellationToken);
        return result.Status ? Ok(result) : NotFound(result);
    }

    private int? GetCurrentUserId()
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claimValue, out var userId) ? userId : null;
    }
}
