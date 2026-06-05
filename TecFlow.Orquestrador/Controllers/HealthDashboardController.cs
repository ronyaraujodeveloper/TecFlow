using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.Orquestrador.Controllers;

[ApiController]
[Route("api/saude")]
public class HealthDashboardController : ControllerBase
{
    private readonly IPlatformHealthService _healthService;

    public HealthDashboardController(IPlatformHealthService healthService)
    {
        _healthService = healthService;
    }

    [HttpGet("dashboard")]
    [AllowAnonymous]
    public async Task<ActionResult<HealthDashboardDto>> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var dashboard = await _healthService.GetDashboardAsync(cancellationToken);
        return Ok(dashboard);
    }
}
