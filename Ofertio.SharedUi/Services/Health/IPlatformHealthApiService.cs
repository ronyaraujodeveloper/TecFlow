using Ofertio.Business.Dto;
using Ofertio.SharedUi.Models;

namespace Ofertio.SharedUi.Services.Health;

public interface IPlatformHealthApiService
{
    Task<ApiResult<HealthDashboardDto>> GetDashboardAsync(CancellationToken cancellationToken = default);
}
