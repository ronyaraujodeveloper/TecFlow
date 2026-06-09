using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Database.Filter;

namespace TecFlow.API.Controllers;

/// <summary>Geração omnichannel de links de afiliado com encurtador TecFlow.</summary>
[ApiController]
[Route("api/affiliate-links")]
[Authorize]
public class AffiliateLinksController : ControllerBase
{
    private readonly IAffiliateLinkGenerationService _generationService;
    private readonly IAffiliateLinkHistoryService _historyService;
    private readonly ILogger<AffiliateLinksController> _logger;

    public AffiliateLinksController(
        IAffiliateLinkGenerationService generationService,
        IAffiliateLinkHistoryService historyService,
        ILogger<AffiliateLinksController> logger)
    {
        _generationService = generationService;
        _historyService = historyService;
        _logger = logger;
    }

    /// <summary>Lista histórico de links gerados pelo usuário com contagem de cliques.</summary>
    [HttpGet("historico")]
    public async Task<ActionResult<AffiliateLinkHistoryResponseDto>> ListHistoryAsync(
        [FromQuery] AffiliateLinkFilter filter,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new AffiliateLinkHistoryResponseDto
            {
                Status = false,
                Descricao = "Usuário não autenticado."
            });
        }

        var result = await _historyService.ListByUserAsync(userId, filter, cancellationToken);
        return result.Status ? Ok(result) : BadRequest(result);
    }

    /// <summary>Gera link de comissão encurtado a partir da URL bruta e loja ativa.</summary>
    [HttpPost("gerar")]
    public async Task<ActionResult<GerarLinkAfiliadoResponseDto>> GenerateAsync(
        [FromBody] GerarLinkAfiliadoDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new GerarLinkAfiliadoResponseDto
            {
                Success = false,
                Message = "Usuário não autenticado."
            });
        }

        try
        {
            var result = await _generationService.GenerateAsync(request, userId, cancellationToken);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao gerar link de afiliado para UserId={UserId}.", userId);
            return StatusCode(500, new GerarLinkAfiliadoResponseDto
            {
                Success = false,
                Message = "Erro inesperado ao gerar link de comissão."
            });
        }
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrWhiteSpace(claim) && int.TryParse(claim, out userId);
    }
}
