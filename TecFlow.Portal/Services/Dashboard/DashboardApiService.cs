using TecFlow.Portal.Models;

using TecFlow.Portal.Models.Responses;

using TecFlow.Portal.Services.Http;



namespace TecFlow.Portal.Services.Dashboard;



public class DashboardApiService : IDashboardApiService

{

    private readonly IHttpService _httpService;



    public DashboardApiService(IHttpService httpService)

    {

        _httpService = httpService;

    }



    public Task<ApiResult<DashboardSummaryDto>> GetStatsAsync(CancellationToken cancellationToken = default) =>

        _httpService.GetAsync<DashboardSummaryDto>("api/Dashboard/stats", cancellationToken);



    public async Task<ApiResult<List<CampaignDto>>> GetCampaignsAsync(CancellationToken cancellationToken = default)

    {

        var result = await _httpService.GetAsync<List<CampaignDto>>("api/Campanhas", cancellationToken);

        if (result.Success && result.Data is not null)

        {

            return ApiResult<List<CampaignDto>>.Ok(result.Data.OrderByDescending(c => c.StartDate).ToList());

        }



        return result.Success

            ? ApiResult<List<CampaignDto>>.Ok([])

            : ApiResult<List<CampaignDto>>.Fail(

                result.ErrorMessage ?? "Erro ao carregar campanhas.",

                result.StatusCode,

                result.ErrorCode,

                result.IsOffline);

    }



    public async Task<ApiResult<List<MetricDto>>> GetMetricsAsync(CancellationToken cancellationToken = default)

    {

        var result = await _httpService.GetAsync<List<MetricDto>>("api/Metricas", cancellationToken);

        if (result.Success && result.Data is not null)

        {

            return ApiResult<List<MetricDto>>.Ok(result.Data.OrderByDescending(m => m.Revenue).ToList());

        }



        return result.Success

            ? ApiResult<List<MetricDto>>.Ok([])

            : ApiResult<List<MetricDto>>.Fail(

                result.ErrorMessage ?? "Erro ao carregar métricas.",

                result.StatusCode,

                result.ErrorCode,

                result.IsOffline);

    }



    public Task<ApiResult<PipelineStatusDto>> GetPipelineStatusAsync(CancellationToken cancellationToken = default) =>

        _httpService.GetAsync<PipelineStatusDto>("api/orquestrador/status", cancellationToken);

}

