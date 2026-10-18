using Ofertio.Business.Dto;
using Ofertio.Core.Entities;
using Ofertio.Core.Enums;

namespace Ofertio.Business.Interfaces.Services;

public interface IAdvertisingProductService
{
    Task<GlobalAdvertisingProduct> CreateGlobalProductAsync(
        int ownerId,
        GlobalAdvertisingProductDto dto,
        CancellationToken cancellationToken = default);

    Task<OptimizedPostPayloadDto> GenerateOptimizedPayloadForPostAsync(
        Guid globalProductId,
        MarketplaceType platform,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GlobalAdvertisingProduct>> GetByOwnerAsync(
        int ownerId,
        CancellationToken cancellationToken = default);
}
