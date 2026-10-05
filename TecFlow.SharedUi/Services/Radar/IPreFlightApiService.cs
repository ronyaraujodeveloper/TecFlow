using TecFlow.Business.Dto;
using TecFlow.SharedUi.Models;
using TecFlow.SharedUi.Services.Http;
using TecFlow.SharedUi.Services.UI;

namespace TecFlow.SharedUi.Services.Radar;

public interface IPreFlightApiService
{
    Task<ApiResult<PreFlightNotificationResponseDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<ApiResult<PreFlightNotificationResponseDto>> SubstituteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class PreFlightApiService : IPreFlightApiService
{
    private readonly IHttpService _http;
    private readonly ILoadingService _loading;

    public PreFlightApiService(IHttpService http, ILoadingService loading)
    {
        _http = http;
        _loading = loading;
    }

    public Task<ApiResult<PreFlightNotificationResponseDto>> ListAsync(CancellationToken cancellationToken = default) =>
        _http.GetAsync<PreFlightNotificationResponseDto>("api/pre-flight", cancellationToken: cancellationToken);

    public Task<ApiResult<PreFlightNotificationResponseDto>> SubstituteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var _ = _loading.BeginScope("Buscando menor preço nas lojas...");
        return _http.PostAsync<object, PreFlightNotificationResponseDto>($"api/pre-flight/{id}/substituir", new { }, cancellationToken);
    }
}
