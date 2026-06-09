using TecFlow.Business.Dto;
using TecFlow.Database.Filter;
using TecFlow.SharedUi.Models;

namespace TecFlow.SharedUi.Services.LinkGenerator;

public interface IAffiliateLinkApiService
{
    Task<ApiResult<GerarLinkAfiliadoResponseDto>> GenerateAsync(
        GerarLinkAfiliadoDto request,
        CancellationToken cancellationToken = default);

    Task<AffiliateLinkHistoryResponseDto> ListHistoryAsync(
        AffiliateLinkFilter filter,
        CancellationToken cancellationToken = default);
}
