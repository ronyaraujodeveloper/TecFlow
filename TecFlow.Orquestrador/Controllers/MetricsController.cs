using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using System.Security.Claims;
using TecFlow.Business.Interfaces.Repositories;



namespace TecFlow.Orquestrador.Controllers;



[ApiController]

[Route("api/Metricas")]

[Authorize]

public class MetricsController : ControllerBase

{

    private readonly IMetricRepository _metricRepository;



    public MetricsController(IMetricRepository metricRepository)

    {

        _metricRepository = metricRepository;

    }



    [HttpGet]

    public async Task<IActionResult> GetAllAsync()

    {

        if (!TryGetUserId(out var userId))

        {

            return Unauthorized();

        }



        var metrics = await _metricRepository.GetByOwnerIdAsync(userId);

        return Ok(metrics);

    }



    [HttpGet("campanha/{campaignId:int}")]

    public async Task<IActionResult> GetByCampaignAsync(int campaignId)

    {

        if (!TryGetUserId(out var userId))

        {

            return Unauthorized();

        }



        var metrics = await _metricRepository.GetByCampaignIdAsync(campaignId);

        if (metrics.Any(m => m.OwnerId != userId))

        {

            return Forbid();

        }



        return Ok(metrics);

    }



    private bool TryGetUserId(out int userId)

    {

        userId = 0;

        var claim = User.FindFirst(ClaimTypes.NameIdentifier);

        return claim is not null && int.TryParse(claim.Value, out userId);

    }

}

