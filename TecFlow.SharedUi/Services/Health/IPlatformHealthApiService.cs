using TecFlow.Business.Dto;
using TecFlow.SharedUi.Models;

namespace TecFlow.SharedUi.Services.Health;

public interface IPlatformHealthApiService
{
    Task<ApiResult<HealthDashboardDto>> GetDashboardAsync(CancellationToken cancellationToken = default);
}
