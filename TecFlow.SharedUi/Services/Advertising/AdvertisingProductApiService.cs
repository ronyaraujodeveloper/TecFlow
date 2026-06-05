using TecFlow.Business.Dto;
using TecFlow.Core.Enums;
using TecFlow.SharedUi.Models;
using TecFlow.SharedUi.Services.Http;

namespace TecFlow.SharedUi.Services.Advertising;

public class AdvertisingProductApiService : IAdvertisingProductApiService
{
    private readonly IHttpService _httpService;

    public AdvertisingProductApiService(IHttpService httpService)
    {
        _httpService = httpService;
    }

    public Task<ApiResult<GlobalAdvertisingProductResponseDto>> ListAsync(
        CancellationToken cancellationToken = default) =>
        _httpService.GetAsync<GlobalAdvertisingProductResponseDto>(
            "api/propaganda/produtos",
            cancellationToken: cancellationToken);

    public Task<ApiResult<GlobalAdvertisingProductResponseDto>> CreateAsync(
        GlobalAdvertisingProductDto dto,
        CancellationToken cancellationToken = default) =>
        _httpService.PostAsync<GlobalAdvertisingProductDto, GlobalAdvertisingProductResponseDto>(
            "api/propaganda/produtos",
            dto,
            cancellationToken);

    public Task<ApiResult<GlobalAdvertisingProductResponseDto>> GetPayloadAsync(
        Guid globalProductId,
        MarketplaceType platform,
        CancellationToken cancellationToken = default) =>
        _httpService.GetAsync<GlobalAdvertisingProductResponseDto>(
            $"api/propaganda/produtos/{globalProductId}/payload?platform={(int)platform}",
            cancellationToken: cancellationToken);
}
