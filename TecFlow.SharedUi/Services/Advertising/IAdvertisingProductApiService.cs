using TecFlow.Business.Dto;
using TecFlow.Core.Enums;
using TecFlow.SharedUi.Models;

namespace TecFlow.SharedUi.Services.Advertising;

public interface IAdvertisingProductApiService
{
    Task<ApiResult<GlobalAdvertisingProductResponseDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<ApiResult<GlobalAdvertisingProductResponseDto>> CreateAsync(
        GlobalAdvertisingProductDto dto,
        CancellationToken cancellationToken = default);
    Task<ApiResult<GlobalAdvertisingProductResponseDto>> GetPayloadAsync(
        Guid globalProductId,
        MarketplaceType platform,
        CancellationToken cancellationToken = default);
}
