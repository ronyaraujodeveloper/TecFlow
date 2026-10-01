using TecFlow.Business.Dto;

namespace TecFlow.Business.Interfaces.Services;

public interface ITelegramBroadcastService
{
    Task<TelegramBroadcastResponseDto> ListCampaignsAsync(int userId, CancellationToken cancellationToken = default);

    Task<TelegramBroadcastResponseDto> ScheduleAsync(
        int userId,
        TelegramScheduleCampaignDto request,
        CancellationToken cancellationToken = default);

    Task ProcessDueCampaignsAsync(CancellationToken cancellationToken = default);
}
