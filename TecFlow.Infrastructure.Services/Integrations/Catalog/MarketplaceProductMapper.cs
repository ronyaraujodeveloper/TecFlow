using System.Globalization;
using TecFlow.Business.Dto;
using TecFlow.Business.Integrations.Shopee.Payloads;
using TecFlow.Business.Integrations.TikTokShop.Payloads;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;

namespace TecFlow.Infrastructure.Services.Integrations.Catalog;

internal static class MarketplaceProductMapper
{
    public static ProductResponseDto ToProductResponseDto(
        bool status,
        string descricao,
        List<Product> products) =>
        new()
        {
            Status = status,
            Descricao = descricao,
            DataList = products
        };

    public static List<Product> MapShopee(
        MarketplaceType marketplace,
        ShopeeGetItemBaseInfoResponsePayload payload,
        IReadOnlyDictionary<long, ShopeeGetModelListResponsePayload>? modelsByItemId)
    {
        var result = new List<Product>();
        if (payload.ItemList is null)
        {
            return result;
        }

        foreach (var item in payload.ItemList)
        {
            if (item.HasModel &&
                modelsByItemId is not null &&
                modelsByItemId.TryGetValue(item.ItemId, out var modelPayload) &&
                modelPayload.Model is { Count: > 0 })
            {
                foreach (var model in modelPayload.Model)
                {
                    result.Add(MapShopeeModel(item, model, marketplace));
                }
            }
            else
            {
                result.Add(MapShopeeItem(item, marketplace));
            }
        }

        return result;
    }

    public static List<Product> MapTikTok(
        MarketplaceType marketplace,
        TikTokShopProductsSearchResponsePayload payload)
    {
        var result = new List<Product>();
        if (payload.Products is null)
        {
            return result;
        }

        foreach (var product in payload.Products)
        {
            if (product.Skus is { Count: > 0 })
            {
                foreach (var sku in product.Skus)
                {
                    result.Add(MapTikTokSku(product, sku, marketplace));
                }
            }
            else
            {
                result.Add(MapTikTokProduct(product, marketplace));
            }
        }

        return result;
    }

    private static Product MapShopeeItem(ShopeeItemBaseInfo item, MarketplaceType marketplace) =>
        new()
        {
            ExternalProductId = item.ItemId.ToString(),
            SkuCode = string.IsNullOrWhiteSpace(item.ItemSku) ? item.ItemId.ToString() : item.ItemSku,
            Name = item.ItemName ?? string.Empty,
            Description = item.Description ?? string.Empty,
            Summary = Truncate(item.Description, 200),
            Category = item.CategoryId.ToString(),
            Price = ResolveShopeePrice(item.PriceInfo),
            Stock = ResolveShopeeStock(item.StockInfoV2),
            MainImageUrl = item.Image?.ImageUrlList?.FirstOrDefault(),
            ImageUrls = item.Image?.ImageUrlList?.ToList() ?? new List<string>(),
            Features = FormatShopeeAttributes(item.AttributeList),
            MarketplaceSource = marketplace,
            ModifiedOn = DateTime.UtcNow,
            CreatedOn = DateTime.UtcNow
        };

    private static Product MapShopeeModel(
        ShopeeItemBaseInfo item,
        ShopeeProductModel model,
        MarketplaceType marketplace)
    {
        var sku = string.IsNullOrWhiteSpace(model.ModelSku)
            ? $"{item.ItemId}-{model.ModelId}"
            : model.ModelSku;

        return new Product
        {
            ExternalProductId = item.ItemId.ToString(),
            SkuCode = sku,
            Name = item.ItemName ?? string.Empty,
            Description = item.Description ?? string.Empty,
            Summary = Truncate(item.Description, 200),
            Category = item.CategoryId.ToString(),
            Price = ResolveShopeePrice(model.PriceInfo ?? item.PriceInfo),
            Stock = ResolveShopeeStock(model.StockInfoV2 ?? item.StockInfoV2),
            MainImageUrl = item.Image?.ImageUrlList?.FirstOrDefault(),
            ImageUrls = item.Image?.ImageUrlList?.ToList() ?? new List<string>(),
            Features = FormatShopeeAttributes(item.AttributeList),
            MarketplaceSource = marketplace,
            ModifiedOn = DateTime.UtcNow,
            CreatedOn = DateTime.UtcNow
        };
    }

    private static Product MapTikTokSku(
        TikTokShopProduct product,
        TikTokShopProductSku sku,
        MarketplaceType marketplace)
    {
        var productId = product.ResolveProductId();
        var skuId = sku.ResolveSkuId();
        var skuCode = string.IsNullOrWhiteSpace(sku.SellerSku) ? skuId : sku.SellerSku;

        return new Product
        {
            ExternalProductId = productId,
            SkuCode = skuCode,
            Name = product.Title ?? string.Empty,
            Description = product.Description ?? string.Empty,
            Summary = Truncate(product.Description, 200),
            Category = ResolveTikTokCategory(product.CategoryChains),
            Price = ResolveTikTokPrice(sku.Price),
            Stock = ResolveTikTokStock(sku.Inventory),
            MainImageUrl = ResolveTikTokImage(product.MainImages),
            ImageUrls = product.MainImages?
                .SelectMany(i => (IEnumerable<string>)(i.Urls ?? i.ThumbUrlList ?? Enumerable.Empty<string>()))
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Distinct()
                .ToList() ?? new List<string>(),
            Features = FormatTikTokAttributes(product.Attributes),
            MarketplaceSource = marketplace,
            ModifiedOn = DateTime.UtcNow,
            CreatedOn = DateTime.UtcNow
        };
    }

    private static Product MapTikTokProduct(TikTokShopProduct product, MarketplaceType marketplace)
    {
        var productId = product.ResolveProductId();
        return new Product
        {
            ExternalProductId = productId,
            SkuCode = productId,
            Name = product.Title ?? string.Empty,
            Description = product.Description ?? string.Empty,
            Summary = Truncate(product.Description, 200),
            Category = ResolveTikTokCategory(product.CategoryChains),
            MainImageUrl = ResolveTikTokImage(product.MainImages),
            ImageUrls = product.MainImages?
                .SelectMany(i => (IEnumerable<string>)(i.Urls ?? i.ThumbUrlList ?? Enumerable.Empty<string>()))
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Distinct()
                .ToList() ?? new List<string>(),
            Features = FormatTikTokAttributes(product.Attributes),
            MarketplaceSource = marketplace,
            ModifiedOn = DateTime.UtcNow,
            CreatedOn = DateTime.UtcNow
        };
    }

    private static decimal ResolveShopeePrice(List<ShopeePriceInfo>? priceInfo)
    {
        if (priceInfo is null || priceInfo.Count == 0)
        {
            return 0;
        }

        var first = priceInfo[0];
        return first.CurrentPrice > 0 ? first.CurrentPrice : first.OriginalPrice;
    }

    private static int ResolveShopeeStock(ShopeeStockInfoV2? stockInfo)
    {
        if (stockInfo?.SummaryInfo is not null)
        {
            return stockInfo.SummaryInfo.TotalAvailableStock;
        }

        return stockInfo?.SellerStock?.Sum(s => s.Stock) ?? 0;
    }

    private static decimal ResolveTikTokPrice(TikTokShopSkuPrice? price)
    {
        if (price is null)
        {
            return 0;
        }

        if (decimal.TryParse(price.SalePrice, NumberStyles.Number, CultureInfo.InvariantCulture, out var sale))
        {
            return sale;
        }

        if (decimal.TryParse(price.TaxExclusivePrice, NumberStyles.Number, CultureInfo.InvariantCulture, out var taxExclusive))
        {
            return taxExclusive;
        }

        return decimal.TryParse(price.OriginalPrice, NumberStyles.Number, CultureInfo.InvariantCulture, out var original)
            ? original
            : 0;
    }

    private static int ResolveTikTokStock(List<TikTokShopSkuInventory>? inventory)
    {
        if (inventory is null || inventory.Count == 0)
        {
            return 0;
        }

        return inventory.Sum(i => i.AvailableStock > 0 ? i.AvailableStock : i.Quantity);
    }

    private static string ResolveTikTokCategory(List<TikTokShopCategoryChain>? chains)
    {
        if (chains is null || chains.Count == 0)
        {
            return string.Empty;
        }

        var leaf = chains.LastOrDefault(c => c.IsLeaf);
        return leaf?.LocalName ?? chains[^1].LocalName ?? string.Empty;
    }

    private static string? ResolveTikTokImage(List<TikTokShopProductImage>? images) =>
        images?
            .SelectMany(i => (IEnumerable<string>)(i.Urls ?? i.ThumbUrlList ?? Enumerable.Empty<string>()))
            .FirstOrDefault(u => !string.IsNullOrWhiteSpace(u))
        ?? images?.FirstOrDefault()?.Uri;

    private static string FormatShopeeAttributes(List<ShopeeItemAttribute>? attributes)
    {
        if (attributes is null || attributes.Count == 0)
        {
            return string.Empty;
        }

        return string.Join("; ",
            attributes.Select(a =>
            {
                var values = a.AttributeValueList?
                    .Select(v => v.OriginalValueName)
                    .Where(v => !string.IsNullOrWhiteSpace(v));
                return $"{a.AttributeName}: {string.Join(", ", values ?? Array.Empty<string?>())}";
            }));
    }

    private static string FormatTikTokAttributes(List<TikTokShopProductAttribute>? attributes)
    {
        if (attributes is null || attributes.Count == 0)
        {
            return string.Empty;
        }

        return string.Join("; ",
            attributes.Select(a =>
            {
                var values = a.Values?.Select(v => v.Name).Where(v => !string.IsNullOrWhiteSpace(v));
                return $"{a.Name}: {string.Join(", ", values ?? Array.Empty<string?>())}";
            }));
    }

    private static string Truncate(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
        {
            return text ?? string.Empty;
        }

        return text[..maxLength];
    }
}
