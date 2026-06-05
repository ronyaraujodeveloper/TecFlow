using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TecFlow.Business.Dto;
using TecFlow.Infrastructure.Services.Services;


namespace TecFlow.Orquestrador.Controllers;

[ApiController]

[Route("api/[controller]")]

[Authorize]

public class DashboardController : ControllerBase

{

    private readonly IAnaliseCalculoService _dashboardAnalyticsService;



    public DashboardController(IAnaliseCalculoService dashboardAnalyticsService)

    {

        _dashboardAnalyticsService = dashboardAnalyticsService;

    }



    [HttpGet("stats")]

    public async Task<ActionResult<DashboardSummaryDto>> GetDashboardStatsAsync()

    {

        if (!TryGetUserId(out var userId))

        {

            return Unauthorized(new { Message = "Token invÃ¡lido ou utilizador nÃ£o identificado." });

        }



        var stats = await _dashboardAnalyticsService.CalculateDashboardStatisticsAsync(userId);

        return Ok(stats ?? new DashboardSummaryDto());

    }



    private bool TryGetUserId(out int userId)

    {

        userId = 0;

        var claim = User.FindFirst(ClaimTypes.NameIdentifier);

        return claim is not null && int.TryParse(claim.Value, out userId);

    }

}

