using TecFlow.Business.Dto;

namespace TecFlow.Business.Interfaces.Services;

public interface ITelegramApiService
{
    Task<TelegramBotIdentityDto?> ValidateBotTokenAsync(
        string botToken,
        CancellationToken cancellationToken = default);

    Task<bool> RegisterWebhookAsync(
        string botToken,
        string webhookUrl,
        string secretToken,
        CancellationToken cancellationToken = default);

    /// <summary>Registra o webhook e relança falhas do Telegram (localhost/HTTP) para o caller tratar.</summary>
    Task<bool> SetWebhookAsync(
        string botToken,
        string webhookUrl,
        string secretToken,
        CancellationToken cancellationToken = default);

    Task<bool> SendTextMessageAsync(
        string botToken,
        string chatId,
        string text,
        CancellationToken cancellationToken = default);

    Task<bool> SendPhotoMessageAsync(
        string botToken,
        string chatId,
        string imageUrl,
        string? caption,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TelegramGroupDto>> FetchAdminChatsAsync(
        string botToken,
        IEnumerable<string>? seedChatIds,
        CancellationToken cancellationToken = default);
}
