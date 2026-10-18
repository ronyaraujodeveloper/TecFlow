using Ofertio.Business.Dto;
using Ofertio.Core.Enums;
using Ofertio.SharedUi.Models;

namespace Ofertio.SharedUi.Services.Advertising;

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
