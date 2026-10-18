using Ofertio.Business.Dto;

namespace Ofertio.Business.Interfaces.Services;

public interface IPlatformHealthService
{
    Task<HealthDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}
