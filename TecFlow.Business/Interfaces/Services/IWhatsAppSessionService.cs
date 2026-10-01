using TecFlow.Business.Dto;

namespace TecFlow.Business.Interfaces.Services;

public interface IWhatsAppSessionService
{
    Task<WhatsAppIntegrationResponseDto> GetMineAsync(int userId, CancellationToken cancellationToken = default);

    Task<WhatsAppIntegrationResponseDto> ConnectAsync(int userId, CancellationToken cancellationToken = default);

    Task<WhatsAppIntegrationResponseDto> RefreshStatusAsync(int userId, CancellationToken cancellationToken = default);

    Task<WhatsAppIntegrationResponseDto> UpdateBotPreferencesAsync(
        int userId,
        WhatsAppBotPreferencesDto preferences,
        CancellationToken cancellationToken = default);
}
