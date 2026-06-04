using System.Text.Json.Serialization;

namespace TecFlow.Business.Integrations.Shopee.Payloads;

public class ShopeeGetItemListResponsePayload
{
    [JsonPropertyName("item")]
    public List<ShopeeItemListEntry>? Item { get; set; }

    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }

    [JsonPropertyName("has_next_page")]
    public bool HasNextPage { get; set; }

    [JsonPropertyName("next_offset")]
    public int NextOffset { get; set; }
}

public class ShopeeItemListEntry
{
    [JsonPropertyName("item_id")]
    public long ItemId { get; set; }

    [JsonPropertyName("item_status")]
    public string? ItemStatus { get; set; }

    [JsonPropertyName("update_time")]
    public long UpdateTime { get; set; }
}
