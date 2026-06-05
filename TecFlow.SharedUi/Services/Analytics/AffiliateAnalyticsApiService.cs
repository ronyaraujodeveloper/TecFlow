using TecFlow.Business.Dto;
using TecFlow.Database.Filter;
using TecFlow.SharedUi.Models;
using TecFlow.SharedUi.Services.Http;

namespace TecFlow.SharedUi.Services.Analytics;

public class AffiliateAnalyticsApiService : IAffiliateAnalyticsApiService
{
    private readonly IHttpService _httpService;

    public AffiliateAnalyticsApiService(IHttpService httpService)
    {
        _httpService = httpService;
    }

    public Task<ApiResult<AffiliateReconciliationResponseDto>> GetReconciliationAsync(
        AffiliateReconciliationFilter filter,
        CancellationToken cancellationToken = default) =>
        _httpService.GetAsync<AffiliateReconciliationResponseDto>(
            "api/afiliados/analytics/conciliacao",
            filter,
            cancellationToken);
}
