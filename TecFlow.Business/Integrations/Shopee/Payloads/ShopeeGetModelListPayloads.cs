using System.Text.Json.Serialization;

namespace TecFlow.Business.Integrations.Shopee.Payloads;

public class ShopeeGetModelListResponsePayload
{
    [JsonPropertyName("tier_variation")]
    public List<ShopeeTierVariation>? TierVariation { get; set; }

    [JsonPropertyName("model")]
    public List<ShopeeProductModel>? Model { get; set; }
}

public class ShopeeTierVariation
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("option_list")]
    public List<ShopeeTierOption>? OptionList { get; set; }
}

public class ShopeeTierOption
{
    [JsonPropertyName("option")]
    public string? Option { get; set; }
}

public class ShopeeProductModel
{
    [JsonPropertyName("model_id")]
    public long ModelId { get; set; }

    [JsonPropertyName("model_sku")]
    public string? ModelSku { get; set; }

    [JsonPropertyName("model_status")]
    public string? ModelStatus { get; set; }

    [JsonPropertyName("price_info")]
    public List<ShopeePriceInfo>? PriceInfo { get; set; }

    [JsonPropertyName("stock_info_v2")]
    public ShopeeStockInfoV2? StockInfoV2 { get; set; }

    [JsonPropertyName("tier_index")]
    public List<int>? TierIndex { get; set; }
}
