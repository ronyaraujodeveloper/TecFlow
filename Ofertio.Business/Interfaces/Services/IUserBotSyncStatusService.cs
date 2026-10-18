using Ofertio.Business.Dto;

namespace Ofertio.Business.Interfaces.Services;

public interface IUserBotSyncStatusService
{
    void MarkRunning(int userId, string? message = null);

    void MarkCompleted(int userId, string? message = null);

    void MarkFailed(int userId, string message);

    UserBotSyncStatusDto Get(int userId);
}
