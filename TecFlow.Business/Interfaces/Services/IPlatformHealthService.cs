using TecFlow.Business.Dto;

namespace TecFlow.Business.Interfaces.Services;

public interface IPlatformHealthService
{
    Task<HealthDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}
