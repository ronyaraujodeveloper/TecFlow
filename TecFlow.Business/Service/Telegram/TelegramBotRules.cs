using System.Text.Json;
using TecFlow.Business.Service.Security;
using TecFlow.Business.Service.WhatsApp;

namespace TecFlow.Business.Service.Telegram;

public sealed class TelegramIncomingMessage
{
    public long UpdateId { get; set; }

    public string ChatId { get; set; } = string.Empty;

    public string ChatType { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public bool FromBot { get; set; }

    public bool IsPrivate { get; set; }

    public bool IsGroup { get; set; }

    public string ChatTitle { get; set; } = string.Empty;

    public string? MessageId { get; set; }
}

public static class TelegramBotRules
{
    public const string TelegramBotSource = "TelegramBot";
    public const int ReplyTimeoutMilliseconds = 1800;
    public const string ConnectedLabel = "Conectado ✅";
    public const string DispatchModeLabel = "Conectado (Modo Disparo)";
    public const string DisconnectedLabel = "Desconectado";
    public const string AwaitingChannelMessageLabel = "Aguardando mensagem no canal...";
    public const string MissingTokenMessage = "Por favor, informe a token válida emitida pelo @BotFather.";
    public const string InvalidTokenMessage = "Token do Telegram inválida";
    public const string DispatchModeSavedMessage =
        "Conexão salva! O webhook para escuta automática não pôde ser ativado em ambiente local, mas os disparos agendados para o Chat ID informado funcionarão normalmente.";

    public static bool IsTelegramBotSource(string? source) =>
        string.Equals(source, TelegramBotSource, StringComparison.OrdinalIgnoreCase);

    public static bool IsPlaceholderToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return true;
        }

        var trimmed = token.Trim();
        if (trimmed == SecretMasking.MaskedValue)
        {
            return true;
        }

        foreach (var character in trimmed)
        {
            if (character != '*')
            {
                return false;
            }
        }

        return trimmed.Length > 0;
    }

    public static bool IsWebhookRegistered(string? sessionData)
    {
        if (string.IsNullOrWhiteSpace(sessionData))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(sessionData);
            var root = document.RootElement;
            if (root.TryGetProperty("webhookRegistered", out var registered))
            {
                return registered.ValueKind == JsonValueKind.True;
            }

            return root.TryGetProperty("webhookUrl", out var url)
                && !string.IsNullOrWhiteSpace(url.GetString());
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static string ResolveUiStatus(bool isActive, bool hasToken, bool hasBotUsername, bool hasChatId, bool webhookRegistered)
    {
        if (!isActive || !hasToken || !hasBotUsername)
        {
            return DisconnectedLabel;
        }

        if (webhookRegistered)
        {
            return ConnectedLabel;
        }

        return hasChatId ? DispatchModeLabel : DisconnectedLabel;
    }

    public static TelegramIncomingMessage? TryParseIncoming(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var message = ReadElement(payload, "message");
        if (message.ValueKind != JsonValueKind.Object)
        {
            message = ReadElement(payload, "edited_message");
        }

        if (message.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var chat = ReadElement(message, "chat");
        var from = ReadElement(message, "from");
        var chatId = ReadChatId(chat);
        var chatType = (ReadString(chat, "type") ?? string.Empty).Trim().ToLowerInvariant();
        var text = ReadString(message, "text")
            ?? ReadString(message, "caption")
            ?? string.Empty;

        return new TelegramIncomingMessage
        {
            UpdateId = ReadInt64(payload, "update_id"),
            ChatId = chatId,
            ChatType = chatType,
            Text = text,
            FromBot = ReadBool(from, "is_bot"),
            IsPrivate = chatType is "private",
            IsGroup = chatType is "group" or "supergroup" or "channel",
            ChatTitle = ReadString(chat, "title") ?? string.Empty,
            MessageId = ReadInt64(message, "message_id").ToString()
        };
    }

    public static bool ShouldIgnore(TelegramIncomingMessage message) =>
        message.FromBot
        || !message.IsPrivate
        || string.IsNullOrWhiteSpace(message.ChatId)
        || string.IsNullOrWhiteSpace(message.Text);

    public static bool ShouldCaptureGroup(TelegramIncomingMessage message) =>
        !message.FromBot
        && message.IsGroup
        && !string.IsNullOrWhiteSpace(message.ChatId)
        && !string.IsNullOrWhiteSpace(message.Text);

    public static IReadOnlyList<string> ExtractUrls(string? text) =>
        WhatsAppBotRules.ExtractUrls(text);

    public static string FormatConvertedReply(IEnumerable<string> convertedLinks) =>
        WhatsAppBotRules.FormatConvertedReply(convertedLinks);

    private static JsonElement ReadElement(JsonElement parent, string name)
    {
        if (parent.ValueKind != JsonValueKind.Object)
        {
            return default;
        }

        foreach (var property in parent.EnumerateObject())
        {
            if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }

        return default;
    }

    private static string? ReadString(JsonElement parent, params string[] names)
    {
        if (parent.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var name in names)
        {
            var element = ReadElement(parent, name);
            if (element.ValueKind == JsonValueKind.String)
            {
                return element.GetString();
            }
        }

        return null;
    }

    private static string ReadChatId(JsonElement chat)
    {
        var id = ReadElement(chat, "id");
        if (id.ValueKind == JsonValueKind.Number && id.TryGetInt64(out var number))
        {
            return number.ToString();
        }

        return ReadString(chat, "id") ?? string.Empty;
    }

    private static long ReadInt64(JsonElement parent, string name)
    {
        var element = ReadElement(parent, name);
        return element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out var value)
            ? value
            : 0;
    }

    private static bool ReadBool(JsonElement parent, string name)
    {
        var element = ReadElement(parent, name);
        if (element.ValueKind == JsonValueKind.True)
        {
            return true;
        }

        if (element.ValueKind == JsonValueKind.False)
        {
            return false;
        }

        return false;
    }
}
