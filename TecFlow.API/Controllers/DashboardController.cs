using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Database.Filter;
using TecFlow.Infrastructure.Services.Services;

namespace TecFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IAnaliseCalculoService _dashboardAnalyticsService;
    private readonly IIntegracaoLojaRepository _integracaoLojaRepository;

    public DashboardController(
        IAnaliseCalculoService dashboardAnalyticsService,
        IIntegracaoLojaRepository integracaoLojaRepository)
    {
        _dashboardAnalyticsService = dashboardAnalyticsService
            ?? throw new ArgumentNullException(nameof(dashboardAnalyticsService));
        _integracaoLojaRepository = integracaoLojaRepository
            ?? throw new ArgumentNullException(nameof(integracaoLojaRepository));
    }

    [HttpGet("stats")]
    public async Task<ActionResult<DashboardSummaryDto>> GetDashboardStatsAsync(
        [FromQuery] DashboardAnalyticsFilter filter,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized("Token inválido ou usuário não identificado.");
        }

        if (filter.LojaId.HasValue)
        {
            var loja = await _integracaoLojaRepository.GetByIdAsync(filter.LojaId.Value, cancellationToken);
            if (loja is null || loja.UserId != userId)
            {
                return BadRequest(new { message = "Loja informada não pertence ao usuário autenticado." });
            }
        }

        try
        {
            var stats = await _dashboardAnalyticsService.CalculateDashboardStatisticsAsync(userId, filter.LojaId);
            return Ok(stats ?? new DashboardSummaryDto());
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Erro ao processar estatísticas do dashboard." });
        }
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null && int.TryParse(claim.Value, out userId);
    }
}
