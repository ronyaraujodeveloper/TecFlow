using System.Text.Json.Serialization;

namespace TecFlow.Business.Integrations.Shopee.Payloads;

public class ShopeeProductApiEnvelope<T>
{
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("warning")]
    public string? Warning { get; set; }

    [JsonPropertyName("request_id")]
    public string? RequestId { get; set; }

    [JsonPropertyName("response")]
    public T? Response { get; set; }

    public bool IsSuccess => string.IsNullOrEmpty(Error);
}
