using TecFlow.Business.Dto;
using TecFlow.SharedUi.Models;
using TecFlow.SharedUi.Services.Http;
using TecFlow.SharedUi.Services.UI;

namespace TecFlow.SharedUi.Services.Integrations;

public interface ITelegramIntegrationApiService
{
    Task<ApiResult<TelegramIntegrationResponseDto>> GetMineAsync(CancellationToken cancellationToken = default);

    Task<ApiResult<TelegramIntegrationResponseDto>> SaveAsync(
        SaveTelegramIntegrationDto request,
        CancellationToken cancellationToken = default);

    Task<ApiResult<TelegramIntegrationResponseDto>> ConnectAsync(
        SaveTelegramIntegrationDto request,
        CancellationToken cancellationToken = default);

    Task<ApiResult<TelegramBroadcastResponseDto>> ListCampaignsAsync(CancellationToken cancellationToken = default);

    Task<ApiResult<TelegramBroadcastResponseDto>> SyncChannelsAsync(CancellationToken cancellationToken = default);

    Task<ApiResult<TelegramBroadcastResponseDto>> ScheduleCampaignAsync(
        TelegramScheduleCampaignDto request,
        CancellationToken cancellationToken = default);
}

public sealed class TelegramIntegrationApiService : ITelegramIntegrationApiService
{
    public const string Path = "api/integracoes/telegram";

    private readonly IHttpService _httpService;
    private readonly ILoadingService _loadingService;

    public TelegramIntegrationApiService(IHttpService httpService, ILoadingService loadingService)
    {
        _httpService = httpService;
        _loadingService = loadingService;
    }

    public Task<ApiResult<TelegramIntegrationResponseDto>> GetMineAsync(
        CancellationToken cancellationToken = default) =>
        _httpService.GetAsync<TelegramIntegrationResponseDto>(Path, cancellationToken: cancellationToken);

    public Task<ApiResult<TelegramIntegrationResponseDto>> SaveAsync(
        SaveTelegramIntegrationDto request,
        CancellationToken cancellationToken = default)
    {
        using var _ = _loadingService.BeginScope("Salvando Telegram...");
        return _httpService.PutAsync<SaveTelegramIntegrationDto, TelegramIntegrationResponseDto>(
            Path,
            request,
            cancellationToken);
    }

    public Task<ApiResult<TelegramIntegrationResponseDto>> ConnectAsync(
        SaveTelegramIntegrationDto request,
        CancellationToken cancellationToken = default)
    {
        using var _ = _loadingService.BeginScope("Conectando Telegram...");
        return _httpService.PostAsync<SaveTelegramIntegrationDto, TelegramIntegrationResponseDto>(
            $"{Path}/conectar",
            request,
            cancellationToken);
    }

    public Task<ApiResult<TelegramBroadcastResponseDto>> ListCampaignsAsync(
        CancellationToken cancellationToken = default) =>
        _httpService.GetAsync<TelegramBroadcastResponseDto>($"{Path}/campanhas", cancellationToken: cancellationToken);

    public Task<ApiResult<TelegramBroadcastResponseDto>> SyncChannelsAsync(CancellationToken cancellationToken = default)
    {
        using var _ = _loadingService.BeginScope("Sincronizando canais do Telegram...");
        return _httpService.PostAsync<object, TelegramBroadcastResponseDto>(
            $"{Path}/canais/sincronizar",
            new { },
            cancellationToken);
    }

    public Task<ApiResult<TelegramBroadcastResponseDto>> ScheduleCampaignAsync(
        TelegramScheduleCampaignDto request,
        CancellationToken cancellationToken = default)
    {
        using var _ = _loadingService.BeginScope("Agendando publicação Telegram...");
        return _httpService.PostAsync<TelegramScheduleCampaignDto, TelegramBroadcastResponseDto>(
            $"{Path}/campanhas",
            request,
            cancellationToken);
    }
}
