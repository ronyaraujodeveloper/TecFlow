using TecFlow.Portal.Models;

using TecFlow.Portal.Models.Responses;



namespace TecFlow.Portal.Services.Dashboard;



public interface IDashboardApiService

{

    Task<ApiResult<DashboardSummaryDto>> GetStatsAsync(CancellationToken cancellationToken = default);

    Task<ApiResult<List<CampaignDto>>> GetCampaignsAsync(CancellationToken cancellationToken = default);

    Task<ApiResult<List<MetricDto>>> GetMetricsAsync(CancellationToken cancellationToken = default);

    Task<ApiResult<PipelineStatusDto>> GetPipelineStatusAsync(CancellationToken cancellationToken = default);

}

