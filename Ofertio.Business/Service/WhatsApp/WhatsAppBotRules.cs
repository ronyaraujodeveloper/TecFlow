using System.Text.Json;
using Ofertio.Business.Service.Groups;
using Ofertio.Business.Service.Telegram;

namespace Ofertio.Business.Service.WhatsApp;

public sealed class WhatsAppIncomingMessage
{
    public string InstanceName { get; set; } = string.Empty;

    public string RemoteJid { get; set; } = string.Empty;

    public bool FromMe { get; set; }

    public string Text { get; set; } = string.Empty;

    public bool IsGroup { get; set; }

    public string? ImageUrl { get; set; }

    public string? MessageId { get; set; }

    public string? GroupName { get; set; }

    /// <summary>Horário de envio (<c>messageTimestamp</c>, Unix) em UTC.</summary>
    public DateTime? SentAtUtc { get; set; }
}

/// <summary>Regras do bot WhatsApp: MESSAGES_UPSERT, fromMe, URLs e escopo privado/grupo.</summary>
public static class WhatsAppBotRules
{
    public const string WhatsAppBotSource = "WhatsAppBot";
    public const int ReplyTimeoutMilliseconds = 2800;

    public static bool IsWhatsAppBotSource(string? source) =>
        string.Equals(source, WhatsAppBotSource, StringComparison.OrdinalIgnoreCase);

    public static bool IsMessagesUpsert(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        var eventName = ReadString(payload, "event", "Event") ?? string.Empty;
        return eventName.Contains("MESSAGES_UPSERT", StringComparison.OrdinalIgnoreCase)
            || eventName.Contains("messages.upsert", StringComparison.OrdinalIgnoreCase);
    }

    public static WhatsAppIncomingMessage? TryParseIncoming(JsonElement payload)
    {
        if (!IsMessagesUpsert(payload))
        {
            return null;
        }

        var instance = ReadString(payload, "instance", "instanceName") ?? string.Empty;
        var data = ReadElement(payload, "data");
        if (data.ValueKind == JsonValueKind.Array && data.GetArrayLength() > 0)
        {
            data = data[0];
        }

        if (data.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var key = ReadElement(data, "key");
        var fromMe = ReadBool(key, "fromMe") || ReadBool(data, "fromMe");
        var remoteJid = ReadString(key, "remoteJid", "remoteJidAlt")
            ?? ReadString(data, "remoteJid")
            ?? string.Empty;
        var text = ReadMessageText(data);
        var message = ReadElement(data, "message");

        return new WhatsAppIncomingMessage
        {
            InstanceName = instance.Trim(),
            RemoteJid = remoteJid.Trim(),
            FromMe = fromMe,
            Text = text,
            IsGroup = remoteJid.Contains("@g.us", StringComparison.OrdinalIgnoreCase),
            ImageUrl = ReadString(ReadElement(message, "imageMessage"), "url")
                ?? ReadString(data, "mediaUrl"),
            MessageId = ReadString(key, "id"),
            GroupName = ReadString(data, "pushName", "notifyName"),
            SentAtUtc = CapturedMessageTimeRules.FromUnixSeconds(ReadUnixSeconds(ReadElement(data, "messageTimestamp")))
        };
    }

    /// <summary>Baileys envia o timestamp como número, string ou Long protobuf (<c>{ low, high }</c>).</summary>
    private static long ReadUnixSeconds(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Number when element.TryGetInt64(out var number):
                return number;
            case JsonValueKind.String when long.TryParse(element.GetString(), out var parsed):
                return parsed;
            case JsonValueKind.Object:
                var low = ReadElement(element, "low");
                var high = ReadElement(element, "high");
                if (low.ValueKind == JsonValueKind.Number && low.TryGetInt64(out var lowValue))
                {
                    var highValue = high.ValueKind == JsonValueKind.Number && high.TryGetInt64(out var h) ? h : 0;
                    return (highValue << 32) | (uint)lowValue;
                }

                return 0;
            default:
                return 0;
        }
    }

    public static bool ShouldIgnore(WhatsAppIncomingMessage message) =>
        message.FromMe
        || string.IsNullOrWhiteSpace(message.InstanceName)
        || string.IsNullOrWhiteSpace(message.RemoteJid);

    public static bool ShouldHandleChat(
        bool enableBot,
        bool replyPrivate,
        bool replyGroups,
        bool isGroup)
    {
        if (!enableBot)
        {
            return false;
        }

        return isGroup ? replyGroups : replyPrivate;
    }

    public static IReadOnlyList<string> ExtractUrls(string? text) =>
        TelegramUserMonitorRules.ExtractHttpUrls(text);

    public static string FormatConvertedReply(IEnumerable<string> convertedLinks)
    {
        var links = convertedLinks
            .Where(link => !string.IsNullOrWhiteSpace(link))
            .Select(link => link.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (links.Count == 0)
        {
            return string.Empty;
        }

        return "Link de comissão Ofertio:\n" + string.Join("\n", links);
    }

    private static string ReadMessageText(JsonElement data)
    {
        var message = ReadElement(data, "message");
        if (message.ValueKind != JsonValueKind.Object)
        {
            return ReadString(data, "conversation", "text", "body") ?? string.Empty;
        }

        return ReadString(message, "conversation")
            ?? ReadString(ReadElement(message, "extendedTextMessage"), "text")
            ?? ReadString(ReadElement(message, "imageMessage"), "caption")
            ?? ReadString(ReadElement(message, "documentMessage"), "caption")
            ?? ReadString(ReadElement(message, "videoMessage"), "caption")
            ?? string.Empty;
    }

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

        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var number))
        {
            return number != 0;
        }

        if (element.ValueKind == JsonValueKind.String
            && bool.TryParse(element.GetString(), out var parsed))
        {
            return parsed;
        }

        return false;
    }
}
