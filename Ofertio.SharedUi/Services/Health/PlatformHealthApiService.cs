using Ofertio.Business.Dto;
using Ofertio.SharedUi.Models;
using Ofertio.SharedUi.Services.Http;

namespace Ofertio.SharedUi.Services.Health;

public class PlatformHealthApiService : IPlatformHealthApiService
{
    private readonly IHttpService _httpService;

    public PlatformHealthApiService(IHttpService httpService)
    {
        _httpService = httpService;
    }

    public Task<ApiResult<HealthDashboardDto>> GetDashboardAsync(CancellationToken cancellationToken = default) =>
        _httpService.GetAsync<HealthDashboardDto>("api/saude/dashboard", cancellationToken: cancellationToken);
}
