using System.Text.Json.Serialization;

namespace TecFlow.Business.Integrations.Shopee.Payloads;

/// <summary>Push Mechanism — code 3 = order status update.</summary>
public class ShopeePushNotificationPayload
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("shop_id")]
    public long ShopId { get; set; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("data")]
    public ShopeeOrderStatusPushData? Data { get; set; }
}

public class ShopeeOrderStatusPushData
{
    [JsonPropertyName("ordersn")]
    public string? OrderSn { get; set; }

    [JsonPropertyName("order_sn")]
    public string? OrderSnAlt { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    public string ResolveOrderSn() =>
        !string.IsNullOrWhiteSpace(OrderSn) ? OrderSn! : OrderSnAlt ?? string.Empty;
}
