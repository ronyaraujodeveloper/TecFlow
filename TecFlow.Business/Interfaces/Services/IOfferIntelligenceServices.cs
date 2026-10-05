using TecFlow.Business.Dto;

namespace TecFlow.Business.Interfaces.Services;

public interface IOfferHealthService
{
    Task<OfferHealthAlertResponseDto> ListAsync(int userId, CancellationToken cancellationToken = default);

    Task<int> InspectRecentCampaignsAsync(int userId, CancellationToken cancellationToken = default);
}

public interface IGroupAttributionService
{
    Task<GroupAttributionResponseDto> ListAsync(int userId, CancellationToken cancellationToken = default);
}

public interface IEvergreenLibraryService
{
    Task<EvergreenLibraryResponseDto> ListAsync(int userId, CancellationToken cancellationToken = default);

    Task<EvergreenLibraryResponseDto> RefreshAsync(int userId, CancellationToken cancellationToken = default);

    Task<EvergreenLibraryResponseDto> RecycleAsync(int userId, CancellationToken cancellationToken = default);
}

public interface IOfferMediaStudio
{
    Task<OfferMediaResponseDto> ApplyFrameAsync(
        int userId,
        OfferMediaRequestDto request,
        CancellationToken cancellationToken = default);

    Task<OfferMediaResponseDto> ExtractVideoAsync(
        OfferMediaRequestDto request,
        CancellationToken cancellationToken = default);
}

public interface IOfferIntelligenceEngine
{
    Task RunCycleAsync(CancellationToken cancellationToken = default);
}
