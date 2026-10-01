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
}
