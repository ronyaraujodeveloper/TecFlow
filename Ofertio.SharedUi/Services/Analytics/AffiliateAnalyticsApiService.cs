using Ofertio.Business.Dto;
using Ofertio.Database.Filter;
using Ofertio.SharedUi.Models;
using Ofertio.SharedUi.Services.Http;

namespace Ofertio.SharedUi.Services.Analytics;

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
