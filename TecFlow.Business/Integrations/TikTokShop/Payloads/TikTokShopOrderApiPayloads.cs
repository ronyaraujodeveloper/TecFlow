using System.Text.Json.Serialization;

namespace TecFlow.Business.Integrations.TikTokShop.Payloads;

public class TikTokShopOrdersSearchResponsePayload
{
    [JsonPropertyName("orders")]
    public List<TikTokShopOrderSummary>? Orders { get; set; }

    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }

    [JsonPropertyName("next_page_token")]
    public string? NextPageToken { get; set; }
}

public class TikTokShopOrderSummary
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("order_id")]
    public string? OrderId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("update_time")]
    public long UpdateTime { get; set; }

    [JsonPropertyName("line_items")]
    public List<TikTokShopOrderLineItem>? LineItems { get; set; }

    public string ResolveOrderId() =>
        !string.IsNullOrWhiteSpace(Id) ? Id! : OrderId ?? string.Empty;
}

public class TikTokShopOrderLineItem
{
    [JsonPropertyName("sku_id")]
    public string? SkuId { get; set; }

    [JsonPropertyName("seller_sku")]
    public string? SellerSku { get; set; }

    [JsonPropertyName("product_id")]
    public string? ProductId { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    public string ResolveSku() =>
        !string.IsNullOrWhiteSpace(SellerSku) ? SellerSku! : SkuId ?? string.Empty;
}

public class TikTokShopUpdateStocksRequestPayload
{
    [JsonPropertyName("skus")]
    public List<TikTokShopStockUpdateSku> Skus { get; set; } = new();
}

public class TikTokShopStockUpdateSku
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("sku_id")]
    public string? SkuId { get; set; }

    [JsonPropertyName("inventory")]
    public List<TikTokShopSkuInventory> Inventory { get; set; } = new();

    public string ResolveSkuId() =>
        !string.IsNullOrWhiteSpace(Id) ? Id! : SkuId ?? string.Empty;
}
