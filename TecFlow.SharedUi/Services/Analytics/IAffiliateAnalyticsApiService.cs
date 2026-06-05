using TecFlow.Business.Dto;
using TecFlow.Database.Filter;
using TecFlow.SharedUi.Models;

namespace TecFlow.SharedUi.Services.Analytics;

public interface IAffiliateAnalyticsApiService
{
    Task<ApiResult<AffiliateReconciliationResponseDto>> GetReconciliationAsync(
        AffiliateReconciliationFilter filter,
        CancellationToken cancellationToken = default);
}
