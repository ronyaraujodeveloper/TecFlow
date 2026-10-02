using TecFlow.Business.Dto;

namespace TecFlow.Business.Interfaces.Services;

public interface IWhatsAppBroadcastService
{
    Task<WhatsAppBroadcastResponseDto> ListGroupsAsync(int userId, CancellationToken cancellationToken = default);

    Task<WhatsAppBroadcastResponseDto> SyncGroupsAsync(int userId, CancellationToken cancellationToken = default);

    Task<WhatsAppBroadcastResponseDto> ListCampaignsAsync(int userId, CancellationToken cancellationToken = default);

    Task<WhatsAppBroadcastResponseDto> ScheduleAsync(
        int userId,
        WhatsAppScheduleCampaignDto request,
        CancellationToken cancellationToken = default);

    Task<WhatsAppBroadcastResponseDto> DeleteCampaignAsync(
        int userId,
        int campaignId,
        CancellationToken cancellationToken = default);

    Task ProcessDueCampaignsAsync(CancellationToken cancellationToken = default);
}
