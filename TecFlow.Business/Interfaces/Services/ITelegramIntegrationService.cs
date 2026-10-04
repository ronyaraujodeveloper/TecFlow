using TecFlow.Business.Dto;

namespace TecFlow.Business.Interfaces.Services;

public interface ITelegramIntegrationService
{
    Task<TelegramIntegrationResponseDto> GetMineAsync(int userId, CancellationToken cancellationToken = default);

    Task<TelegramIntegrationResponseDto> SaveAsync(
        int userId,
        SaveTelegramIntegrationDto request,
        CancellationToken cancellationToken = default);

    Task<TelegramIntegrationResponseDto> ConnectAsync(
        int userId,
        SaveTelegramIntegrationDto request,
        CancellationToken cancellationToken = default);

    Task<TelegramIntegrationResponseDto> RequestUserBotCodeAsync(
        int userId,
        SaveTelegramIntegrationDto request,
        CancellationToken cancellationToken = default);

    Task<TelegramIntegrationResponseDto> ConfirmUserBotAsync(
        int userId,
        SaveTelegramIntegrationDto request,
        CancellationToken cancellationToken = default);
}
