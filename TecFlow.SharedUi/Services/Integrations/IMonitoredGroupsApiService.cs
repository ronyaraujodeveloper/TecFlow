using TecFlow.Business.Dto;
using TecFlow.SharedUi.Extensions;
using TecFlow.SharedUi.Models;
using TecFlow.SharedUi.Services.Http;
using TecFlow.SharedUi.Services.UI;

namespace TecFlow.SharedUi.Services.Integrations;

public interface IMonitoredGroupsApiService
{
    Task<ApiResult<MonitoredGroupsResponseDto>> ListAsync(
        int hours,
        string? groupKey,
        string? channel,
        CancellationToken cancellationToken = default);

    Task<ApiResult<MonitoredGroupsResponseDto>> SyncAsync(string? channel, CancellationToken cancellationToken = default);

    Task<ApiResult<MonitoredGroupsResponseDto>> ValidateAsync(
        int offerId,
        string? channel,
        CancellationToken cancellationToken = default);

    Task<ApiResult<MonitoredGroupsResponseDto>> CloneAsync(
        int offerId,
        string? channel,
        CancellationToken cancellationToken = default);
}

public sealed class MonitoredGroupsApiService : IMonitoredGroupsApiService
{
    public const string Path = "api/integracoes/grupos/monitorados";

    private readonly IHttpService _httpService;
    private readonly ILoadingService _loadingService;

    public MonitoredGroupsApiService(IHttpService httpService, ILoadingService loadingService)
    {
        _httpService = httpService;
        _loadingService = loadingService;
    }

    public Task<ApiResult<MonitoredGroupsResponseDto>> ListAsync(
        int hours,
        string? groupKey,
        string? channel,
        CancellationToken cancellationToken = default) =>
        _httpService.GetAsync<MonitoredGroupsResponseDto>(
            Path,
            new { hours, groupKey, channel },
            cancellationToken);

    public Task<ApiResult<MonitoredGroupsResponseDto>> SyncAsync(
        string? channel,
        CancellationToken cancellationToken = default)
    {
        using var _ = _loadingService.BeginScope("Sincronizando grupos monitorados...");
        return _httpService.PostAsync<object, MonitoredGroupsResponseDto>(
            $"{Path}/sincronizar".AppendQueryString(new { channel }),
            new { },
            cancellationToken);
    }

    public Task<ApiResult<MonitoredGroupsResponseDto>> ValidateAsync(
        int offerId,
        string? channel,
        CancellationToken cancellationToken = default)
    {
        using var _ = _loadingService.BeginScope("Validando anúncio...");
        return _httpService.PostAsync<object, MonitoredGroupsResponseDto>(
            $"{Path}/{offerId}/validar".AppendQueryString(new { channel }),
            new { },
            cancellationToken);
    }

    public Task<ApiResult<MonitoredGroupsResponseDto>> CloneAsync(
        int offerId,
        string? channel,
        CancellationToken cancellationToken = default)
    {
        using var _ = _loadingService.BeginScope("Convertendo link de comissão...");
        return _httpService.PostAsync<object, MonitoredGroupsResponseDto>(
            $"{Path}/{offerId}/clonar".AppendQueryString(new { channel }),
            new { },
            cancellationToken);
    }
}
