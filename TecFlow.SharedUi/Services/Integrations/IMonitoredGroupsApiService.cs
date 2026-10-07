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
        int skip = 0,
        int take = 25,
        bool ignored = false,
        CancellationToken cancellationToken = default);

    Task<ApiResult<MonitoredGroupsResponseDto>> SyncAsync(string? channel, CancellationToken cancellationToken = default);

    Task<ApiResult<MonitoredGroupsResponseDto>> ResetAndResyncAsync(
        string? channel,
        CancellationToken cancellationToken = default);

    Task<ApiResult<UserBotSyncStatusDto>> GetSyncStatusAsync(CancellationToken cancellationToken = default);

    Task<ApiResult<MonitoredGroupsResponseDto>> PrioritizeMediaAsync(
        IReadOnlyList<int> offerIds,
        CancellationToken cancellationToken = default);

    Task<ApiResult<MonitoredGroupsResponseDto>> LinkExistingImagesAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResult<MonitoredGroupsResponseDto>> ValidateAsync(
        int offerId,
        string? channel,
        CancellationToken cancellationToken = default);

    Task<ApiResult<MonitoredGroupsResponseDto>> CloneAsync(
        int offerId,
        string? channel,
        CancellationToken cancellationToken = default);

    Task<ApiResult<MonitoredGroupsResponseDto>> IgnoreAsync(
        int offerId,
        string? channel,
        CancellationToken cancellationToken = default);

    Task<ApiResult<MonitoredGroupsResponseDto>> RestoreAsync(
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
        int skip = 0,
        int take = 25,
        bool ignored = false,
        CancellationToken cancellationToken = default) =>
        _httpService.GetAsync<MonitoredGroupsResponseDto>(
            Path,
            new { hours, groupKey, channel, skip, take, ignored },
            cancellationToken);

    public Task<ApiResult<MonitoredGroupsResponseDto>> SyncAsync(
        string? channel,
        CancellationToken cancellationToken = default) =>
        _httpService.PostAsync<object, MonitoredGroupsResponseDto>(
            $"{Path}/sincronizar".AppendQueryString(new { channel }),
            new { },
            cancellationToken);

    public Task<ApiResult<MonitoredGroupsResponseDto>> ResetAndResyncAsync(
        string? channel,
        CancellationToken cancellationToken = default) =>
        _httpService.PostAsync<object, MonitoredGroupsResponseDto>(
            $"{Path}/resetar".AppendQueryString(new { channel }),
            new { },
            cancellationToken);

    public Task<ApiResult<UserBotSyncStatusDto>> GetSyncStatusAsync(CancellationToken cancellationToken = default) =>
        _httpService.GetAsync<UserBotSyncStatusDto>($"{Path}/status", cancellationToken: cancellationToken);

    public Task<ApiResult<MonitoredGroupsResponseDto>> PrioritizeMediaAsync(
        IReadOnlyList<int> offerIds,
        CancellationToken cancellationToken = default) =>
        _httpService.PostAsync<PrioritizeMonitoredMediaRequest, MonitoredGroupsResponseDto>(
            $"{Path}/midia/priorizar",
            new PrioritizeMonitoredMediaRequest { OfferIds = offerIds.ToList() },
            cancellationToken);

    public Task<ApiResult<MonitoredGroupsResponseDto>> LinkExistingImagesAsync(
        CancellationToken cancellationToken = default) =>
        _httpService.PostAsync<object, MonitoredGroupsResponseDto>(
            $"{Path}/midia/vincular-disco",
            new { },
            cancellationToken);

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

    public Task<ApiResult<MonitoredGroupsResponseDto>> IgnoreAsync(
        int offerId,
        string? channel,
        CancellationToken cancellationToken = default)
    {
        using var _ = _loadingService.BeginScope("Ocultando oferta...");
        return _httpService.PostAsync<object, MonitoredGroupsResponseDto>(
            $"{Path}/{offerId}/ignorar".AppendQueryString(new { channel }),
            new { },
            cancellationToken);
    }

    public Task<ApiResult<MonitoredGroupsResponseDto>> RestoreAsync(
        int offerId,
        string? channel,
        CancellationToken cancellationToken = default)
    {
        using var _ = _loadingService.BeginScope("Restaurando oferta...");
        return _httpService.PostAsync<object, MonitoredGroupsResponseDto>(
            $"{Path}/{offerId}/restaurar".AppendQueryString(new { channel }),
            new { },
            cancellationToken);
    }
}
