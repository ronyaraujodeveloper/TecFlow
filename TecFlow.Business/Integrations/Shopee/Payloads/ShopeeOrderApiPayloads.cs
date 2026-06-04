using System.Text.Json.Serialization;

namespace TecFlow.Business.Integrations.Shopee.Payloads;

public class ShopeeGetOrderListResponsePayload
{
    [JsonPropertyName("order_list")]
    public List<ShopeeOrderListEntry>? OrderList { get; set; }

    [JsonPropertyName("more")]
    public bool More { get; set; }

    [JsonPropertyName("next_cursor")]
    public string? NextCursor { get; set; }
}

public class ShopeeOrderListEntry
{
    [JsonPropertyName("order_sn")]
    public string? OrderSn { get; set; }

    [JsonPropertyName("order_status")]
    public string? OrderStatus { get; set; }

    [JsonPropertyName("update_time")]
    public long UpdateTime { get; set; }
}

public class ShopeeGetOrderDetailResponsePayload
{
    [JsonPropertyName("order_list")]
    public List<ShopeeOrderDetail>? OrderList { get; set; }
}

public class ShopeeOrderDetail
{
    [JsonPropertyName("order_sn")]
    public string? OrderSn { get; set; }

    [JsonPropertyName("order_status")]
    public string? OrderStatus { get; set; }

    [JsonPropertyName("item_list")]
    public List<ShopeeOrderItem>? ItemList { get; set; }
}

public class ShopeeOrderItem
{
    [JsonPropertyName("item_id")]
    public long ItemId { get; set; }

    [JsonPropertyName("model_id")]
    public long ModelId { get; set; }

    [JsonPropertyName("model_sku")]
    public string? ModelSku { get; set; }

    [JsonPropertyName("item_sku")]
    public string? ItemSku { get; set; }

    [JsonPropertyName("model_quantity_purchased")]
    public int ModelQuantityPurchased { get; set; }

    public string ResolveSku() =>
        !string.IsNullOrWhiteSpace(ModelSku)
            ? ModelSku!
            : !string.IsNullOrWhiteSpace(ItemSku)
                ? ItemSku!
                : ModelId > 0
                    ? $"{ItemId}-{ModelId}"
                    : ItemId.ToString();
}

public class ShopeeUpdateStockRequestPayload
{
    [JsonPropertyName("item_id")]
    public long ItemId { get; set; }

    [JsonPropertyName("stock_list")]
    public List<ShopeeStockUpdateEntry> StockList { get; set; } = new();
}

public class ShopeeStockUpdateEntry
{
    [JsonPropertyName("model_id")]
    public long ModelId { get; set; }

    [JsonPropertyName("seller_stock")]
    public List<ShopeeSellerStockUpdate> SellerStock { get; set; } = new();
}

public class ShopeeSellerStockUpdate
{
    [JsonPropertyName("location_id")]
    public string LocationId { get; set; } = string.Empty;

    [JsonPropertyName("stock")]
    public int Stock { get; set; }
}
