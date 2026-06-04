using TecFlow.Business.Dto;
using TecFlow.Database.Filter;
using TecFlow.WebUi.Models;
using TecFlow.WebUi.Models.Responses;
using TecFlow.WebUi.Services.Http;

namespace TecFlow.WebUi.Services.Dashboard;

public interface IDashboardApiService
{
    Task<ApiResult<DashboardSummaryDto>> GetStatsAsync(CancellationToken cancellationToken = default);

    Task<ApiResult<CampaignResponseDto>> GetCampaignsByFilterAsync(
        CampaignFilter filter,
        CancellationToken cancellationToken = default);

    Task<ApiResult<MetricResponseDto>> GetMetricsByFilterAsync(
        MetricFilter filter,
        CancellationToken cancellationToken = default);

    Task<ApiResult<CampaignResponseDto>> CreateCampaignAsync(
        CampaignDto dto,
        CancellationToken cancellationToken = default);

    Task<ApiResult<MetricResponseDto>> CreateMetricAsync(
        MetricDto dto,
        CancellationToken cancellationToken = default);

    Task<ApiResult<PipelineStatusDto>> GetPipelineStatusAsync(CancellationToken cancellationToken = default);
}
