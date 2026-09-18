using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
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
    private readonly ILogger<IntegracoesController> _logger;
    private readonly IHostEnvironment _hostEnvironment;

    public IntegracoesController(
        IIntegracaoLojaService integracaoLojaService,
        ILogger<IntegracoesController> logger,
        IHostEnvironment hostEnvironment)
    {
        _integracaoLojaService = integracaoLojaService;
        _logger = logger;
        _hostEnvironment = hostEnvironment;
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
            return Unauthorized(IntegracaoLojaFail("Usuário não autenticado."));
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
        if (!ModelState.IsValid)
        {
            return InvalidModelState();
        }

        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(IntegracaoLojaFail("Usuário não autenticado."));
        }

        if (dto is null)
        {
            return BadRequest(IntegracaoLojaFail("Payload de vinculação inválido."));
        }

        ApplyHomologFallbacks(dto);

        try
        {
            var result = await _integracaoLojaService.LinkAsync(userId.Value, dto, cancellationToken);
            return result.Status ? Ok(result) : BadRequest(result);
        }
        catch (Exception)
        {
            return BadRequest(IntegracaoLojaFail("Não foi possível vincular a loja. Tente novamente."));
        }
    }

    /// <summary>Vinculação manual (homologação) no contrato MarketplaceAccountResponseDto.</summary>
    [HttpPost("/api/marketplace-auth/vincular-manual")]
    public async Task<ActionResult<MarketplaceAccountResponseDto>> VincularManualAsync(
        [FromBody] IntegracaoLojaDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return InvalidModelState();
        }

        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(MarketplaceAccountResponseDto.Fail("Usuário não autenticado."));
        }

        if (dto is null)
        {
            return BadRequest(MarketplaceAccountResponseDto.Fail("Payload de vinculação inválido."));
        }

        ApplyHomologFallbacks(dto);

        try
        {
            var result = await _integracaoLojaService.LinkAsync(userId.Value, dto, cancellationToken);
            if (!result.Status)
            {
                return BadRequest(MarketplaceAccountResponseDto.Fail(result.Descricao));
            }

            return Ok(MarketplaceAccountResponseDto.Ok(result.Data));
        }
        catch (Exception)
        {
            return BadRequest(MarketplaceAccountResponseDto.Fail("Não foi possível vincular a loja. Tente novamente."));
        }
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
            return Unauthorized(IntegracaoLojaFail("Usuário não autenticado."));
        }

        var result = await _integracaoLojaService.UnlinkAsync(userId.Value, id, cancellationToken);
        return result.Status ? Ok(result) : NotFound(result);
    }

    private BadRequestObjectResult InvalidModelState()
    {
        var fields = string.Join("; ", ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .Select(entry =>
            {
                var messages = entry.Value!.Errors.Select(error =>
                    string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? error.Exception?.Message ?? "inválido"
                        : error.ErrorMessage);
                return $"{entry.Key}: {string.Join(", ", messages)}";
            }));

        _logger.LogWarning("ModelState inválido ao vincular loja. Campos: {Fields}", fields);
        return BadRequest(MarketplaceAccountResponseDto.Fail($"Payload de vinculação inválido. {fields}"));
    }

    private void ApplyHomologFallbacks(IntegracaoLojaDto dto)
    {
        if (!_hostEnvironment.IsDevelopment() && !_hostEnvironment.IsEnvironment("Homologacao"))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(dto.AuthorizationCode))
        {
            dto.AuthorizationCode = "code_teste";
        }

        if (string.IsNullOrWhiteSpace(dto.ShopId))
        {
            dto.ShopId = "123456";
        }
    }

    private static IntegracaoLojaResponseDto IntegracaoLojaFail(string descricao) =>
        new()
        {
            Status = false,
            Descricao = descricao
        };

    private int? GetCurrentUserId()
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claimValue, out var userId) ? userId : null;
    }
}
