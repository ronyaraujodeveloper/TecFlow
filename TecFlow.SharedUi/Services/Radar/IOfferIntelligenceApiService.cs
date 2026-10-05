using TecFlow.Business.Dto;
using TecFlow.SharedUi.Models;
using TecFlow.SharedUi.Services.Http;
using TecFlow.SharedUi.Services.UI;

namespace TecFlow.SharedUi.Services.Radar;

public interface IOfferIntelligenceApiService
{
    Task<ApiResult<OfferHealthAlertResponseDto>> ListHealthAsync(CancellationToken cancellationToken = default);

    Task<ApiResult<GroupAttributionResponseDto>> ListAttributionAsync(CancellationToken cancellationToken = default);

    Task<ApiResult<EvergreenLibraryResponseDto>> ListEvergreenAsync(CancellationToken cancellationToken = default);

    Task<ApiResult<EvergreenLibraryResponseDto>> RefreshEvergreenAsync(CancellationToken cancellationToken = default);

    Task<ApiResult<EvergreenLibraryResponseDto>> RecycleEvergreenAsync(CancellationToken cancellationToken = default);

    Task<ApiResult<OfferMediaResponseDto>> ApplyFrameAsync(OfferMediaRequestDto request, CancellationToken cancellationToken = default);

    Task<ApiResult<OfferMediaResponseDto>> ExtractVideoAsync(OfferMediaRequestDto request, CancellationToken cancellationToken = default);
}

public sealed class OfferIntelligenceApiService : IOfferIntelligenceApiService
{
    private readonly IHttpService _http;
    private readonly ILoadingService _loading;

    public OfferIntelligenceApiService(IHttpService http, ILoadingService loading)
    {
        _http = http;
        _loading = loading;
    }

    public Task<ApiResult<OfferHealthAlertResponseDto>> ListHealthAsync(CancellationToken cancellationToken = default) =>
        _http.GetAsync<OfferHealthAlertResponseDto>("api/inteligencia/saude", cancellationToken: cancellationToken);

    public Task<ApiResult<GroupAttributionResponseDto>> ListAttributionAsync(CancellationToken cancellationToken = default) =>
        _http.GetAsync<GroupAttributionResponseDto>("api/inteligencia/atribuicao", cancellationToken: cancellationToken);

    public Task<ApiResult<EvergreenLibraryResponseDto>> ListEvergreenAsync(CancellationToken cancellationToken = default) =>
        _http.GetAsync<EvergreenLibraryResponseDto>("api/inteligencia/evergreen", cancellationToken: cancellationToken);

    public Task<ApiResult<EvergreenLibraryResponseDto>> RefreshEvergreenAsync(CancellationToken cancellationToken = default)
    {
        using var _ = _loading.BeginScope("Atualizando biblioteca evergreen...");
        return _http.PostAsync<object, EvergreenLibraryResponseDto>("api/inteligencia/evergreen/atualizar", new { }, cancellationToken);
    }

    public Task<ApiResult<EvergreenLibraryResponseDto>> RecycleEvergreenAsync(CancellationToken cancellationToken = default)
    {
        using var _ = _loading.BeginScope("Reciclando ofertas campeãs...");
        return _http.PostAsync<object, EvergreenLibraryResponseDto>("api/inteligencia/evergreen/reciclar", new { }, cancellationToken);
    }

    public Task<ApiResult<OfferMediaResponseDto>> ApplyFrameAsync(OfferMediaRequestDto request, CancellationToken cancellationToken = default)
    {
        using var _ = _loading.BeginScope("Aplicando moldura...");
        return _http.PostAsync<OfferMediaRequestDto, OfferMediaResponseDto>("api/inteligencia/midia/moldura", request, cancellationToken);
    }

    public Task<ApiResult<OfferMediaResponseDto>> ExtractVideoAsync(OfferMediaRequestDto request, CancellationToken cancellationToken = default)
    {
        using var _ = _loading.BeginScope("Baixando vídeo do produto...");
        return _http.PostAsync<OfferMediaRequestDto, OfferMediaResponseDto>("api/inteligencia/midia/video", request, cancellationToken);
    }
}
