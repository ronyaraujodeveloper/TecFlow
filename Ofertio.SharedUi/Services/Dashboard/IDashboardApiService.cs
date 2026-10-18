using Ofertio.Business.Dto;
using Ofertio.Database.Filter;
using Ofertio.SharedUi.Models;
using Ofertio.SharedUi.Models.Responses;
using Ofertio.SharedUi.Services.Http;

namespace Ofertio.SharedUi.Services.Dashboard;

public interface IDashboardApiService
{
    Task<ApiResult<DashboardSummaryDto>> GetStatsAsync(
        DashboardAnalyticsFilter? filter = null,
        CancellationToken cancellationToken = default);

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
