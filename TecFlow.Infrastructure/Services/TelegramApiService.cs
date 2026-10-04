using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Telegram;

namespace TecFlow.Infrastructure.Services;

public sealed class TelegramApiService : ITelegramApiService
{
    private readonly ILogger<TelegramApiService> _logger;

    public TelegramApiService(ILogger<TelegramApiService> logger)
    {
        _logger = logger;
    }

    public async Task<TelegramBotIdentityDto?> ValidateBotTokenAsync(
        string botToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(botToken))
        {
            return null;
        }

        try
        {
            var client = CreateClient(botToken);
            var me = await client.GetMeAsync(cancellationToken);
            return new TelegramBotIdentityDto
            {
                Id = me.Id,
                Username = me.Username,
                FirstName = me.FirstName
            };
        }
        catch (ApiRequestException ex) when (ex.ErrorCode is 400 or 401)
        {
            _logger.LogWarning(ex, "GetMeAsync recusou o BotToken. ErrorCode={ErrorCode}", ex.ErrorCode);
            throw new InvalidOperationException(TelegramBotRules.InvalidTokenMessage, ex);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token Telegram inválido no GetMeAsync.");
            throw new InvalidOperationException(TelegramBotRules.InvalidTokenMessage, ex);
        }
    }

    public async Task<bool> RegisterWebhookAsync(
        string botToken,
        string webhookUrl,
        string secretToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await SetWebhookAsync(botToken, webhookUrl, secretToken, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha no SetWebhookAsync. Url={Url}", webhookUrl);
            return false;
        }
    }

    public async Task<bool> SetWebhookAsync(
        string botToken,
        string webhookUrl,
        string secretToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(webhookUrl))
        {
            throw new InvalidOperationException("Token ou URL do webhook do Telegram ausente.");
        }

        var client = CreateClient(botToken);
        await client.SetWebhookAsync(
            url: webhookUrl,
            secretToken: secretToken,
            cancellationToken: cancellationToken);
        return true;
    }

    public async Task<bool> SendTextMessageAsync(
        string botToken,
        string chatId,
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(chatId) || string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        try
        {
            var client = CreateClient(botToken);
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: text,
                cancellationToken: cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha no SendTextMessageAsync. ChatId={ChatId}", chatId);
            return false;
        }
    }

    public async Task<bool> SendPhotoMessageAsync(
        string botToken,
        string chatId,
        string imageUrl,
        string? caption,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(botToken)
            || string.IsNullOrWhiteSpace(chatId)
            || string.IsNullOrWhiteSpace(imageUrl))
        {
            return false;
        }

        try
        {
            var client = CreateClient(botToken);
            await client.SendPhotoAsync(
                chatId: chatId,
                photo: InputFile.FromUri(imageUrl),
                caption: caption,
                cancellationToken: cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha no SendPhotoAsync. ChatId={ChatId}", chatId);
            return await SendTextMessageAsync(botToken, chatId, caption ?? string.Empty, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<TelegramGroupDto>> FetchAdminChatsAsync(
        string botToken,
        IEnumerable<string>? seedChatIds,
        CancellationToken cancellationToken = default)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var seed in seedChatIds ?? [])
        {
            var trimmed = (seed ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                ids.Add(trimmed);
            }
        }

        var client = CreateClient(botToken);
        try
        {
            var updates = await client.GetUpdatesAsync(
                offset: 0,
                limit: 100,
                timeout: 0,
                cancellationToken: cancellationToken);
            foreach (var update in updates)
            {
                var chat = update.Message?.Chat
                    ?? update.ChannelPost?.Chat
                    ?? update.EditedMessage?.Chat
                    ?? update.MyChatMember?.Chat
                    ?? update.ChatMember?.Chat;
                if (chat is null)
                {
                    continue;
                }

                if (chat.Type is ChatType.Group or ChatType.Supergroup or ChatType.Channel)
                {
                    ids.Add(chat.Id.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetUpdates indisponível para listar canais (webhook ativo ou localhost).");
        }

        var result = new List<TelegramGroupDto>();
        foreach (var chatId in ids)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var chat = await client.GetChatAsync(chatId, cancellationToken);
                if (chat.Type is not (ChatType.Group or ChatType.Supergroup or ChatType.Channel))
                {
                    continue;
                }

                var isAdmin = false;
                var members = 0;
                try
                {
                    var me = await client.GetMeAsync(cancellationToken);
                    var member = await client.GetChatMemberAsync(chat.Id, me.Id, cancellationToken);
                    isAdmin = member.Status is ChatMemberStatus.Administrator or ChatMemberStatus.Creator;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Não foi possível confirmar admin no chat {ChatId}.", chatId);
                }

                try
                {
                    members = await client.GetChatMemberCountAsync(chat.Id, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Não foi possível contar membros do chat {ChatId}.", chatId);
                }

                var name = TelegramBroadcastRules.ResolveFriendlyName(chat.Title, chatId);
                result.Add(new TelegramGroupDto
                {
                    ChatId = chat.Id.ToString(),
                    Name = name,
                    GroupName = name,
                    ParticipantCount = members,
                    IsAdmin = isAdmin,
                    IsActive = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha no GetChat do Telegram. ChatId={ChatId}", chatId);
                result.Add(new TelegramGroupDto
                {
                    ChatId = chatId,
                    Name = TelegramBroadcastRules.UntitledGroupName,
                    GroupName = TelegramBroadcastRules.UntitledGroupName,
                    IsActive = true
                });
            }
        }

        return result;
    }

    private static TelegramBotClient CreateClient(string botToken) =>
        new(botToken.Trim());
}
