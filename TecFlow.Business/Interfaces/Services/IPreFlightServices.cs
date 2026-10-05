using TecFlow.Business.Dto;

namespace TecFlow.Business.Interfaces.Services;

public interface IPreFlightService
{
    Task<PreFlightNotificationResponseDto> ListAsync(int userId, CancellationToken cancellationToken = default);

    Task<int> InspectUpcomingAsync(CancellationToken cancellationToken = default);

    Task<bool> EnsureReadyAsync(string channel, int campaignId, CancellationToken cancellationToken = default);

    Task<PreFlightNotificationResponseDto> SubstituteAsync(
        int userId,
        int notificationId,
        CancellationToken cancellationToken = default);
}

public interface IPreFlightEngine
{
    Task RunCycleAsync(CancellationToken cancellationToken = default);
}
