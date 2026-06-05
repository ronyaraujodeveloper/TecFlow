using TecFlow.Business.Dto;
using TecFlow.Database.Filter;
using TecFlow.SharedUi.Models;
using TecFlow.SharedUi.Models.Responses;
using TecFlow.SharedUi.Services.Http;

namespace TecFlow.SharedUi.Services.Dashboard;

public class DashboardApiService : IDashboardApiService
{
    private readonly IHttpService _httpService;

    public DashboardApiService(IHttpService httpService)
    {
        _httpService = httpService;
    }

    public Task<ApiResult<DashboardSummaryDto>> GetStatsAsync(CancellationToken cancellationToken = default) =>
        _httpService.GetAsync<DashboardSummaryDto>("api/Dashboard/stats", cancellationToken: cancellationToken);

    public Task<ApiResult<CampaignResponseDto>> GetCampaignsByFilterAsync(
        CampaignFilter filter,
        CancellationToken cancellationToken = default) =>
        _httpService.GetAsync<CampaignResponseDto>("api/Campanhas", filter, cancellationToken);

    public Task<ApiResult<MetricResponseDto>> GetMetricsByFilterAsync(
        MetricFilter filter,
        CancellationToken cancellationToken = default) =>
        _httpService.GetAsync<MetricResponseDto>("api/Metricas", filter, cancellationToken);

    public Task<ApiResult<CampaignResponseDto>> CreateCampaignAsync(
        CampaignDto dto,
        CancellationToken cancellationToken = default) =>
        _httpService.PostAsync<CampaignDto, CampaignResponseDto>("api/Campanhas", dto, cancellationToken);

    public Task<ApiResult<MetricResponseDto>> CreateMetricAsync(
        MetricDto dto,
        CancellationToken cancellationToken = default) =>
        _httpService.PostAsync<MetricDto, MetricResponseDto>("api/Metricas", dto, cancellationToken);

    public Task<ApiResult<PipelineStatusDto>> GetPipelineStatusAsync(CancellationToken cancellationToken = default) =>
        _httpService.GetAsync<PipelineStatusDto>("api/orquestrador/status", cancellationToken: cancellationToken);
}
