using TecFlow.Business.Dto;
using TecFlow.SharedUi.Models;
using TecFlow.SharedUi.Services.Http;
using TecFlow.SharedUi.Services.UI;

namespace TecFlow.SharedUi.Services.Radar;

public interface IOfferRadarApiService
{
    Task<ApiResult<AffiliateMiningProfileResponseDto>> GetProfileAsync(CancellationToken cancellationToken = default);

    Task<ApiResult<AffiliateMiningProfileResponseDto>> SaveProfileAsync(
        AffiliateMiningProfileDto request,
        CancellationToken cancellationToken = default);

    Task<ApiResult<OfferArbitrageResponseDto>> SearchArbitrageAsync(
        OfferArbitrageRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ApiResult<OfferRadarResponseDto>> ListAsync(
        int skip = 0,
        int take = 30,
        CancellationToken cancellationToken = default);

    Task<ApiResult<OfferRadarResponseDto>> ScheduleAsync(
        int itemId,
        string? channel,
        CancellationToken cancellationToken = default);

    Task<ApiResult<OfferRadarResponseDto>> QueueAutoPilotAsync(
        int itemId,
        CancellationToken cancellationToken = default);
}

public sealed class OfferRadarApiService : IOfferRadarApiService
{
    private readonly IHttpService _http;
    private readonly ILoadingService _loading;

    public OfferRadarApiService(IHttpService http, ILoadingService loading)
    {
        _http = http;
        _loading = loading;
    }

    public Task<ApiResult<AffiliateMiningProfileResponseDto>> GetProfileAsync(CancellationToken cancellationToken = default) =>
        _http.GetAsync<AffiliateMiningProfileResponseDto>("api/radar/perfil", cancellationToken: cancellationToken);

    public Task<ApiResult<AffiliateMiningProfileResponseDto>> SaveProfileAsync(
        AffiliateMiningProfileDto request,
        CancellationToken cancellationToken = default)
    {
        using var _ = _loading.BeginScope("Salvando perfil de mineração...");
        return _http.PutAsync<AffiliateMiningProfileDto, AffiliateMiningProfileResponseDto>(
            "api/radar/perfil",
            request,
            cancellationToken);
    }

    public Task<ApiResult<OfferArbitrageResponseDto>> SearchArbitrageAsync(
        OfferArbitrageRequestDto request,
        CancellationToken cancellationToken = default)
    {
        using var _ = _loading.BeginScope("Buscando preços menores...");
        return _http.PostAsync<OfferArbitrageRequestDto, OfferArbitrageResponseDto>(
            "api/radar/arbitragem",
            request,
            cancellationToken);
    }

    public Task<ApiResult<OfferRadarResponseDto>> ListAsync(
        int skip = 0,
        int take = 30,
        CancellationToken cancellationToken = default) =>
        _http.GetAsync<OfferRadarResponseDto>("api/radar/ofertas", new { skip, take }, cancellationToken);

    public Task<ApiResult<OfferRadarResponseDto>> ScheduleAsync(
        int itemId,
        string? channel,
        CancellationToken cancellationToken = default)
    {
        using var _ = _loading.BeginScope("Preparando agendamento...");
        return _http.PostAsync<object, OfferRadarResponseDto>(
            $"api/radar/ofertas/{itemId}/agendar?channel={Uri.EscapeDataString(channel ?? "WhatsApp")}",
            new { },
            cancellationToken);
    }

    public Task<ApiResult<OfferRadarResponseDto>> QueueAutoPilotAsync(
        int itemId,
        CancellationToken cancellationToken = default)
    {
        using var _ = _loading.BeginScope("Enviando ao piloto automático...");
        return _http.PostAsync<object, OfferRadarResponseDto>(
            $"api/radar/ofertas/{itemId}/piloto",
            new { },
            cancellationToken);
    }
}
