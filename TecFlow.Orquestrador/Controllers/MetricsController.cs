using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Database.Filter;
using TecFlow.Database.Pagin;

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
    public async Task<ActionResult<MetricResponseDto>> GetByFilterAsync([FromQuery] MetricFilter filter)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new MetricResponseDto { Status = false, Descricao = "Não autorizado." });
        }

        filter.OwnerId ??= userId;

        var filtered = (await _metricRepository.GetByOwnerIdAsync(userId)).ApplyFilter(filter);
        var (items, meta) = PagedListHelper.Slice(filtered, filter);

        return Ok(new MetricResponseDto
        {
            Status = true,
            Descricao = "OK",
            DataList = items,
            Paging = PagingInfoDto.FromMeta(meta)
        });
    }

    [HttpGet("campanha/{campaignId:int}")]
    public async Task<ActionResult<MetricResponseDto>> GetByCampaignAsync(int campaignId)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new MetricResponseDto { Status = false, Descricao = "Não autorizado." });
        }

        var metrics = (await _metricRepository.GetByCampaignIdAsync(campaignId)).ToList();
        if (metrics.Any(m => m.OwnerId != userId))
        {
            return Forbid();
        }

        return Ok(new MetricResponseDto
        {
            Status = true,
            Descricao = "OK",
            DataList = metrics
        });
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null && int.TryParse(claim.Value, out userId);
    }
}
