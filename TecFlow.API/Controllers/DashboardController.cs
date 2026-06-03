using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TecFlow.Business.Dto;
using TecFlow.Infrastructure.Services.Services;

namespace TecFlow.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IAnaliseCalculoService _dashboardAnalyticsService;

        public DashboardController(IAnaliseCalculoService dashboardAnalyticsService)
        {
            _dashboardAnalyticsService = dashboardAnalyticsService ??
                throw new ArgumentNullException(nameof(dashboardAnalyticsService));
        }

        [HttpGet("stats")]
        public async Task<ActionResult<DashboardSummaryDto>> GetDashboardStatsAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized("Token inválido ou usuário não identificado.");
            }

            try
            {
                var stats = await _dashboardAnalyticsService.CalculateDashboardStatisticsAsync(userId);

                if (stats == null)
                {
                    return NotFound(new { message = "Nenhum dado disponível para este usuário." });
                }

                return Ok(stats);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Erro ao processar estatísticas do dashboard." });
            }
        }
    }
}
