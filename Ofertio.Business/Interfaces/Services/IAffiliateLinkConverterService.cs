using Ofertio.Business.Dto;

namespace Ofertio.Business.Interfaces.Services;

public interface IAffiliateLinkConverterService
{
    Task<AffiliateLinkConverterResponseDto> ConvertAsync(
        int userId,
        string capturedUrl,
        string? sourceGroup,
        CancellationToken cancellationToken = default);
}
