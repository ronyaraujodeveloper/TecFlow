using System.Text.Json.Serialization;

namespace TecFlow.Business.Integrations.TikTokShop.Payloads;

public class TikTokShopWebhookPayload
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("event_type")]
    public string? EventType { get; set; }

    [JsonPropertyName("shop_id")]
    public string? ShopId { get; set; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("data")]
    public TikTokShopOrderWebhookData? Data { get; set; }

    public string ResolveEventType() =>
        !string.IsNullOrWhiteSpace(Type) ? Type! : EventType ?? string.Empty;
}

public class TikTokShopOrderWebhookData
{
    [JsonPropertyName("order_id")]
    public string? OrderId { get; set; }

    [JsonPropertyName("order_status")]
    public string? OrderStatus { get; set; }

    [JsonPropertyName("update_time")]
    public long UpdateTime { get; set; }
}
