using System.Text.Json.Serialization;

namespace TecFlow.Business.Integrations.TikTokShop.Payloads;

public class TikTokShopProductsSearchResponsePayload
{
    [JsonPropertyName("products")]
    public List<TikTokShopProduct>? Products { get; set; }

    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }

    [JsonPropertyName("next_page_token")]
    public string? NextPageToken { get; set; }

    [JsonPropertyName("more")]
    public bool More { get; set; }
}

public class TikTokShopProduct
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("product_id")]
    public string? ProductId { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("category_chains")]
    public List<TikTokShopCategoryChain>? CategoryChains { get; set; }

    [JsonPropertyName("attributes")]
    public List<TikTokShopProductAttribute>? Attributes { get; set; }

    [JsonPropertyName("skus")]
    public List<TikTokShopProductSku>? Skus { get; set; }

    [JsonPropertyName("main_images")]
    public List<TikTokShopProductImage>? MainImages { get; set; }

    public string ResolveProductId() =>
        !string.IsNullOrWhiteSpace(Id) ? Id! : ProductId ?? string.Empty;
}

public class TikTokShopCategoryChain
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("parent_id")]
    public string? ParentId { get; set; }

    [JsonPropertyName("local_name")]
    public string? LocalName { get; set; }

    [JsonPropertyName("is_leaf")]
    public bool IsLeaf { get; set; }
}

public class TikTokShopProductAttribute
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("values")]
    public List<TikTokShopAttributeValue>? Values { get; set; }
}

public class TikTokShopAttributeValue
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class TikTokShopProductSku
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("seller_sku")]
    public string? SellerSku { get; set; }

    [JsonPropertyName("sku_id")]
    public string? SkuId { get; set; }

    [JsonPropertyName("price")]
    public TikTokShopSkuPrice? Price { get; set; }

    [JsonPropertyName("inventory")]
    public List<TikTokShopSkuInventory>? Inventory { get; set; }

    public string ResolveSkuId() =>
        !string.IsNullOrWhiteSpace(Id) ? Id! : SkuId ?? string.Empty;
}

public class TikTokShopSkuPrice
{
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("sale_price")]
    public string? SalePrice { get; set; }

    [JsonPropertyName("tax_exclusive_price")]
    public string? TaxExclusivePrice { get; set; }

    [JsonPropertyName("original_price")]
    public string? OriginalPrice { get; set; }
}

public class TikTokShopSkuInventory
{
    [JsonPropertyName("warehouse_id")]
    public string? WarehouseId { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("available_stock")]
    public int AvailableStock { get; set; }
}

public class TikTokShopProductImage
{
    [JsonPropertyName("uri")]
    public string? Uri { get; set; }

    [JsonPropertyName("urls")]
    public List<string>? Urls { get; set; }

    [JsonPropertyName("thumb_url_list")]
    public List<string>? ThumbUrlList { get; set; }
}
