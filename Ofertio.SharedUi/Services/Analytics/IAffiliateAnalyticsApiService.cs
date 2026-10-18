using Ofertio.Business.Dto;
using Ofertio.Database.Filter;
using Ofertio.SharedUi.Models;

namespace Ofertio.SharedUi.Services.Analytics;

public interface IAffiliateAnalyticsApiService
{
    Task<ApiResult<AffiliateReconciliationResponseDto>> GetReconciliationAsync(
        AffiliateReconciliationFilter filter,
        CancellationToken cancellationToken = default);
}
