using Ofertio.Business.Dto;

namespace Ofertio.Business.Interfaces.Services;

public interface ITelegramBroadcastService
{
    Task<TelegramBroadcastResponseDto> ListCampaignsAsync(int userId, CancellationToken cancellationToken = default);

    Task<TelegramBroadcastResponseDto> SyncChannelsAsync(int userId, CancellationToken cancellationToken = default);

    Task<TelegramBroadcastResponseDto> ScheduleAsync(
        int userId,
        TelegramScheduleCampaignDto request,
        CancellationToken cancellationToken = default);

    Task ProcessDueCampaignsAsync(CancellationToken cancellationToken = default);
}
