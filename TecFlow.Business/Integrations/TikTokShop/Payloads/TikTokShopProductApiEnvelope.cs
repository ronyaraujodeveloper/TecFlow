using System.Text.Json.Serialization;

namespace TecFlow.Business.Integrations.TikTokShop.Payloads;

public class TikTokShopProductApiEnvelope<T>
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("request_id")]
    public string? RequestId { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    public bool IsSuccess => Code == 0;
}
