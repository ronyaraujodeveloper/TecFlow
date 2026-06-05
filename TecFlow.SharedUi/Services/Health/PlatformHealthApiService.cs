using TecFlow.Business.Dto;
using TecFlow.SharedUi.Models;
using TecFlow.SharedUi.Services.Http;

namespace TecFlow.SharedUi.Services.Health;

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
