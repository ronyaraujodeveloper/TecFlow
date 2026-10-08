using TecFlow.Business.Dto;
using TecFlow.Database.Filter;
using TecFlow.SharedUi.Models;
using TecFlow.SharedUi.Services.Http;
using TecFlow.SharedUi.Services.UI;

namespace TecFlow.SharedUi.Services.Radar;

public interface IOfficialCatalogSearchApiService
{
    Task<ApiResult<OfficialCatalogSearchResponseDto>> SearchAsync(
        OfficialCatalogSearchFilter filter,
        CancellationToken cancellationToken = default);
}

public sealed class OfficialCatalogSearchApiService : IOfficialCatalogSearchApiService
{
    private readonly IHttpService _http;
    private readonly ILoadingService _loading;

    public OfficialCatalogSearchApiService(IHttpService http, ILoadingService loading)
    {
        _http = http;
        _loading = loading;
    }

    public Task<ApiResult<OfficialCatalogSearchResponseDto>> SearchAsync(
        OfficialCatalogSearchFilter filter,
        CancellationToken cancellationToken = default)
    {
        using var _ = _loading.BeginScope("Buscando nas lojas oficiais...");
        return _http.GetAsync<OfficialCatalogSearchResponseDto>("api/busca", filter, cancellationToken);
    }
}
