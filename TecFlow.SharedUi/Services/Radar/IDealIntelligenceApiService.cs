using TecFlow.Business.Dto;
using TecFlow.SharedUi.Models;
using TecFlow.SharedUi.Services.Http;
using TecFlow.SharedUi.Services.UI;

namespace TecFlow.SharedUi.Services.Radar;

public interface IDealIntelligenceApiService
{
    Task<ApiResult<DealCreditsResponseDto>> ListAsync(
        LiveSearchFilterDto? search = null,
        CancellationToken cancellationToken = default);

    Task<ApiResult<DealCreditsResponseDto>> UnlockAsync(int id, CancellationToken cancellationToken = default);

    Task<ApiResult<DealCreditsResponseDto>> TopUpAsync(int packSize, CancellationToken cancellationToken = default);
}

public sealed class DealIntelligenceApiService : IDealIntelligenceApiService
{
    private readonly IHttpService _http;
    private readonly ILoadingService _loading;

    public DealIntelligenceApiService(IHttpService http, ILoadingService loading)
    {
        _http = http;
        _loading = loading;
    }

    public Task<ApiResult<DealCreditsResponseDto>> ListAsync(
        LiveSearchFilterDto? search = null,
        CancellationToken cancellationToken = default) =>
        _http.GetAsync<DealCreditsResponseDto>(
            "api/achadinhos",
            new
            {
                q = search?.Keyword,
                minPrice = search?.MinPrice,
                maxPrice = search?.MaxPrice,
                coupon = search?.HasCoupon,
                store = search?.Store
            },
            cancellationToken);

    public Task<ApiResult<DealCreditsResponseDto>> UnlockAsync(int id, CancellationToken cancellationToken = default)
    {
        using var _ = _loading.BeginScope("Desbloqueando oferta...");
        return _http.PostAsync<object, DealCreditsResponseDto>($"api/achadinhos/{id}/desbloquear", new { }, cancellationToken);
    }

    public Task<ApiResult<DealCreditsResponseDto>> TopUpAsync(int packSize, CancellationToken cancellationToken = default)
    {
        using var _ = _loading.BeginScope("Adicionando créditos...");
        return _http.PostAsync<TopUpCreditsRequestDto, DealCreditsResponseDto>(
            "api/achadinhos/creditos/pacote",
            new TopUpCreditsRequestDto { PackSize = packSize },
            cancellationToken);
    }
}
