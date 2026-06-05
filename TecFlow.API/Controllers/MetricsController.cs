using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Core.Entities;
using TecFlow.Database.Filter;
using TecFlow.Database.Pagin;

namespace TecFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/Metricas")]
public class MetricsController : ControllerBase
{
    private readonly IMetricRepository _repository;

    public MetricsController(IMetricRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<MetricResponseDto>> GetByFilterAsync([FromQuery] MetricFilter filter)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        filter.OwnerId ??= userId;

        var filtered = (await _repository.GetByOwnerIdAsync(userId)).ApplyFilter(filter);
        var (items, meta) = PagedListHelper.Slice(filtered, filter);

        return Ok(new MetricResponseDto
        {
            Status = true,
            Descricao = "OK",
            DataList = items,
            Paging = PagingInfoDto.FromMeta(meta)
        });
    }

    [HttpPost]
    public async Task<ActionResult<MetricResponseDto>> CreateAsync([FromBody] MetricDto dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var metric = new Metric
        {
            CampaignId = dto.CampaignId,
            Views = dto.Views,
            Clicks = dto.Clicks,
            Sales = dto.Sales,
            Investment = dto.Investment,
            Revenue = dto.Revenue,
            ParentMetricId = dto.ParentMetricId,
            OwnerId = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(metric);
        return Ok(new MetricResponseDto { Status = true, Descricao = "Criada com sucesso.", Data = metric });
    }
}
