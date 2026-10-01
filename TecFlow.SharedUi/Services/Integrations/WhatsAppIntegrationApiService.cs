using TecFlow.Business.Dto;
using TecFlow.SharedUi.Models;
using TecFlow.SharedUi.Services.Http;
using TecFlow.SharedUi.Services.UI;

namespace TecFlow.SharedUi.Services.Integrations;

public interface IWhatsAppIntegrationApiService
{
    Task<ApiResult<WhatsAppIntegrationResponseDto>> GetMineAsync(CancellationToken cancellationToken = default);

    Task<ApiResult<WhatsAppIntegrationResponseDto>> ConnectAsync(CancellationToken cancellationToken = default);

    Task<ApiResult<WhatsAppIntegrationResponseDto>> RefreshStatusAsync(CancellationToken cancellationToken = default);
}

public sealed class WhatsAppIntegrationApiService : IWhatsAppIntegrationApiService
{
    public const string Path = "api/integracoes/whatsapp";

    private readonly IHttpService _httpService;
    private readonly ILoadingService _loadingService;

    public WhatsAppIntegrationApiService(IHttpService httpService, ILoadingService loadingService)
    {
        _httpService = httpService;
        _loadingService = loadingService;
    }

    public Task<ApiResult<WhatsAppIntegrationResponseDto>> GetMineAsync(
        CancellationToken cancellationToken = default) =>
        _httpService.GetAsync<WhatsAppIntegrationResponseDto>(Path, cancellationToken: cancellationToken);

    public Task<ApiResult<WhatsAppIntegrationResponseDto>> ConnectAsync(
        CancellationToken cancellationToken = default)
    {
        using var _ = _loadingService.BeginScope("Gerando conexão WhatsApp...");
        return _httpService.PostAsync<object, WhatsAppIntegrationResponseDto>(
            $"{Path}/conectar",
            new { },
            cancellationToken);
    }

    public Task<ApiResult<WhatsAppIntegrationResponseDto>> RefreshStatusAsync(
        CancellationToken cancellationToken = default) =>
        _httpService.GetAsync<WhatsAppIntegrationResponseDto>($"{Path}/status", cancellationToken: cancellationToken);
}
