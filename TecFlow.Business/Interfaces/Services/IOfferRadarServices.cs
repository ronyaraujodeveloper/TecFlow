using TecFlow.Business.Dto;

namespace TecFlow.Business.Interfaces.Services;

public interface IAffiliateMiningProfileService
{
    Task<AffiliateMiningProfileResponseDto> GetAsync(int userId, CancellationToken cancellationToken = default);

    Task<AffiliateMiningProfileResponseDto> SaveAsync(
        int userId,
        AffiliateMiningProfileDto request,
        CancellationToken cancellationToken = default);
}

public interface IProductArbitrageService
{
    Task<OfferArbitrageResponseDto> SearchAlternativesAsync(
        int userId,
        OfferArbitrageRequestDto request,
        CancellationToken cancellationToken = default);
}

public interface IOfferRadarService
{
    Task<OfferRadarResponseDto> ListAsync(
        int userId,
        int skip = 0,
        int take = 30,
        CancellationToken cancellationToken = default);

    Task<OfferRadarResponseDto> ScheduleAsync(
        int userId,
        int itemId,
        string? channel,
        CancellationToken cancellationToken = default);

    Task<OfferRadarResponseDto> QueueAutoPilotAsync(
        int userId,
        int itemId,
        CancellationToken cancellationToken = default);
}

public interface IOfferMiningEngine
{
    Task<int> MineUserAsync(int userId, CancellationToken cancellationToken = default);
}
