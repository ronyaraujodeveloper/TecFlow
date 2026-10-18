using Ofertio.Business.Dto;
using Ofertio.Database.Filter;
using Ofertio.SharedUi.Models;

namespace Ofertio.SharedUi.Services.LinkGenerator;

public interface IAffiliateLinkApiService
{
    Task<ApiResult<GerarLinkAfiliadoResponseDto>> GenerateAsync(
        GerarLinkAfiliadoDto request,
        CancellationToken cancellationToken = default);

    Task<AffiliateLinkHistoryResponseDto> ListHistoryAsync(
        AffiliateLinkFilter filter,
        CancellationToken cancellationToken = default);

    Task<ApiResult<UpdateAffiliateProductMetadataDto>> UpdateProductMetadataAsync(
        Guid affiliateLinkId,
        UpdateAffiliateProductMetadataDto request,
        CancellationToken cancellationToken = default);
}
