using System.Text.Json.Serialization;

namespace TecFlow.Business.Integrations.Shopee.Payloads;

public class ShopeeGetItemBaseInfoResponsePayload
{
    [JsonPropertyName("item_list")]
    public List<ShopeeItemBaseInfo>? ItemList { get; set; }
}

public class ShopeeItemBaseInfo
{
    [JsonPropertyName("item_id")]
    public long ItemId { get; set; }

    [JsonPropertyName("category_id")]
    public long CategoryId { get; set; }

    [JsonPropertyName("item_name")]
    public string? ItemName { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("item_sku")]
    public string? ItemSku { get; set; }

    [JsonPropertyName("item_status")]
    public string? ItemStatus { get; set; }

    [JsonPropertyName("has_model")]
    public bool HasModel { get; set; }

    [JsonPropertyName("price_info")]
    public List<ShopeePriceInfo>? PriceInfo { get; set; }

    [JsonPropertyName("stock_info_v2")]
    public ShopeeStockInfoV2? StockInfoV2 { get; set; }

    [JsonPropertyName("image")]
    public ShopeeItemImage? Image { get; set; }

    [JsonPropertyName("attribute_list")]
    public List<ShopeeItemAttribute>? AttributeList { get; set; }
}

public class ShopeePriceInfo
{
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("original_price")]
    public decimal OriginalPrice { get; set; }

    [JsonPropertyName("current_price")]
    public decimal CurrentPrice { get; set; }
}

public class ShopeeStockInfoV2
{
    [JsonPropertyName("summary_info")]
    public ShopeeStockSummaryInfo? SummaryInfo { get; set; }

    [JsonPropertyName("seller_stock")]
    public List<ShopeeSellerStock>? SellerStock { get; set; }
}

public class ShopeeStockSummaryInfo
{
    [JsonPropertyName("total_reserved_stock")]
    public int TotalReservedStock { get; set; }

    [JsonPropertyName("total_available_stock")]
    public int TotalAvailableStock { get; set; }
}

public class ShopeeSellerStock
{
    [JsonPropertyName("location_id")]
    public string? LocationId { get; set; }

    [JsonPropertyName("stock")]
    public int Stock { get; set; }
}

public class ShopeeItemImage
{
    [JsonPropertyName("image_url_list")]
    public List<string>? ImageUrlList { get; set; }

    [JsonPropertyName("image_id_list")]
    public List<string>? ImageIdList { get; set; }
}

public class ShopeeItemAttribute
{
    [JsonPropertyName("attribute_id")]
    public long AttributeId { get; set; }

    [JsonPropertyName("attribute_name")]
    public string? AttributeName { get; set; }

    [JsonPropertyName("attribute_value_list")]
    public List<ShopeeAttributeValue>? AttributeValueList { get; set; }
}

public class ShopeeAttributeValue
{
    [JsonPropertyName("value_id")]
    public long ValueId { get; set; }

    [JsonPropertyName("original_value_name")]
    public string? OriginalValueName { get; set; }

    [JsonPropertyName("value_unit")]
    public string? ValueUnit { get; set; }
}
