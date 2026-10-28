using System.Globalization;
using System.Net;
using System.Text.Json;
using Ofertio.Business.Dto;
using Ofertio.Business.Service.Radar;
using Ofertio.Core.Enums;

namespace Ofertio.Business.Integrations.TikTokShop;

/// <summary>
/// Monta a requisição e interpreta a resposta do <c>affiliate_creator/202405/open_collaborations/products/search</c>.
/// A taxa de comissão vem em centésimos de ponto percentual (1000 = 10%).
/// </summary>
public static class TikTokShopAffiliateSearchRules
{
    public const string MissingAccountMessage =
        "API TikTok Shop ainda não autorizada pelo administrador da plataforma";

    public const string MasterTokenExpiredMessage =
        "Token Master do TikTok Shop expirado e a renovação falhou: o administrador precisa reautorizar em Integrações Globais.";

    public const string TimeoutMessage =
        "O TikTok Shop demorou para responder. Tente buscar de novo em instantes.";

    public const string ProductUrlTemplate = "https://shop.tiktok.com/view/product/{0}";

    /// <summary>Códigos de token inválido/expirado do envelope da Open API.</summary>
    private static readonly HashSet<long> TokenErrorCodes = [105000, 105001, 105002, 105005, 36004004];

    private static readonly JsonSerializerOptions BodyOptions = new() { WriteIndented = false };

    public static int ClampPageSize(int pageSize) =>
        pageSize <= 0 ? TikTokShopProductSearchRequest.MaxPageSize : Math.Clamp(pageSize, 1, TikTokShopProductSearchRequest.MaxPageSize);

    public static Dictionary<string, string?> BuildQuery(TikTokShopProductSearchRequest request)
    {
        var query = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["page_size"] = ClampPageSize(request.PageSize).ToString(CultureInfo.InvariantCulture)
        };
        if (!string.IsNullOrWhiteSpace(request.PageToken))
        {
            query["page_token"] = request.PageToken.Trim();
        }

        return query;
    }

    public static string BuildBody(TikTokShopProductSearchRequest request)
    {
        var body = new Dictionary<string, object>(StringComparer.Ordinal);
        var keyword = request.Query?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            body["title_keywords"] = new[] { keyword };
        }

        var priceRange = new Dictionary<string, decimal>(StringComparer.Ordinal);
        if (request.MinPrice is > 0)
        {
            priceRange["amount_ge"] = request.MinPrice.Value;
        }

        if (request.MaxPrice is > 0)
        {
            priceRange["amount_lt"] = request.MaxPrice.Value;
        }

        if (priceRange.Count > 0)
        {
            body["sales_price_range"] = priceRange;
        }

        if (request.MinCommissionRate is > 0)
        {
            body["commission_rate_range"] = new Dictionary<string, int> { ["rate_ge"] = request.MinCommissionRate.Value };
        }

        return JsonSerializer.Serialize(body, BodyOptions);
    }

    public static bool IsTokenRefused(HttpStatusCode statusCode, long? errorCode) =>
        statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
        || (errorCode is not null && TokenErrorCodes.Contains(errorCode.Value));

    public static decimal? CommissionRateToPercent(decimal? rate) =>
        rate is > 0 ? decimal.Round(rate.Value / 100m, 2) : null;

    /// <summary>Lê o envelope <c>{code, message, data}</c>; <paramref name="errorCode"/> é nulo quando <c>code</c> = 0.</summary>
    public static OfficialCatalogChannelResult Parse(string? json, out long? errorCode, out string? errorMessage)
    {
        errorCode = null;
        errorMessage = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            return OfficialCatalogChannelResult.Empty;
        }

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            return OfficialCatalogChannelResult.Empty;
        }

        var code = ReadLong(root, "code");
        if (code is not null and not 0)
        {
            errorCode = code;
            errorMessage = ReadString(root, "message");
            return OfficialCatalogChannelResult.Failed(
                string.IsNullOrWhiteSpace(errorMessage) ? $"TikTok Shop recusou a busca (código {code})." : errorMessage.Trim());
        }

        if (!root.TryGetProperty("data", out var data)
            || data.ValueKind != JsonValueKind.Object
            || !data.TryGetProperty("products", out var products)
            || products.ValueKind != JsonValueKind.Array)
        {
            return OfficialCatalogChannelResult.Empty;
        }

        var items = new List<OfficialCatalogProductDto>();
        foreach (var product in products.EnumerateArray())
        {
            var mapped = MapProduct(product);
            if (mapped is not null)
            {
                items.Add(mapped);
            }
        }

        return OfficialCatalogChannelResult.From(items);
    }

    public static OfficialCatalogProductDto? MapProduct(JsonElement product)
    {
        if (product.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var id = ReadString(product, "id");
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var price = ReadAmount(product, "sales_price");
        var originalPrice = ReadAmount(product, "original_price");
        var commissionRate = product.TryGetProperty("commission", out var commission) && commission.ValueKind == JsonValueKind.Object
            ? ReadDecimal(commission, "rate")
            : null;
        var commissionAmount = commission.ValueKind == JsonValueKind.Object ? ReadDecimal(commission, "amount") : null;
        var commissionPercent = CommissionRateToPercent(commissionRate);
        var unitsSold = ReadLong(product, "units_sold");
        var (categoryId, categoryName, rootId, rootName) = ReadCategories(product);
        var detailLink = ReadString(product, "detail_link");
        var image = ReadString(product, "main_image_url");

        return new OfficialCatalogProductDto
        {
            Platform = nameof(MarketplaceType.TikTokShop),
            PlatformType = MarketplaceType.TikTokShop,
            ProductId = id.Trim(),
            ProductName = ReadString(product, "title"),
            Price = price,
            OriginalPrice = originalPrice is > 0 && price is > 0 && originalPrice > price ? originalPrice : null,
            ImageUrl = image,
            ImageUrls = string.IsNullOrWhiteSpace(image) ? [] : [image],
            SourceUrl = string.IsNullOrWhiteSpace(detailLink)
                ? string.Format(CultureInfo.InvariantCulture, ProductUrlTemplate, id.Trim())
                : detailLink.Trim(),
            Source = "Api",
            CategoryId = categoryId,
            CategoryName = categoryName,
            RootCategoryId = rootId,
            RootCategoryName = rootName,
            CommissionPercent = commissionPercent,
            EstimatedCommission = commissionAmount is > 0 ? decimal.Round(commissionAmount.Value, 2) : null,
            CommissionSource = commissionPercent is not null ? AffiliateCommissionRules.SourceTikTokShopApi : null,
            SoldQuantity = unitsSold is > 0 ? (int)Math.Min(int.MaxValue, unitsSold.Value) : null,
            SellerName = product.TryGetProperty("shop", out var shop) && shop.ValueKind == JsonValueKind.Object
                ? ReadString(shop, "name")
                : null
        };
    }

    private static (string? Id, string? Name, string? RootId, string? RootName) ReadCategories(JsonElement product)
    {
        if (!product.TryGetProperty("category_chains", out var chains) || chains.ValueKind != JsonValueKind.Array)
        {
            return (null, null, null, null);
        }

        var nodes = chains.EnumerateArray()
            .Where(node => node.ValueKind == JsonValueKind.Object)
            .Select(node => (Id: ReadString(node, "id"), Name: ReadString(node, "local_name"), Parent: ReadString(node, "parent_id")))
            .Where(node => !string.IsNullOrWhiteSpace(node.Id))
            .ToList();
        if (nodes.Count == 0)
        {
            return (null, null, null, null);
        }

        var root = nodes.FirstOrDefault(node => string.IsNullOrWhiteSpace(node.Parent) || node.Parent == "0");
        if (root.Id is null)
        {
            root = nodes[0];
        }

        var leaf = nodes[^1];
        return (leaf.Id, leaf.Name, root.Id, root.Name);
    }

    /// <summary>Preço do produto: <c>minimum_amount</c> (faixa de variações) ou <c>amount</c>.</summary>
    private static decimal? ReadAmount(JsonElement product, string property)
    {
        if (!product.TryGetProperty(property, out var price) || price.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var value = ReadDecimal(price, "minimum_amount") ?? ReadDecimal(price, "amount");
        return value is > 0 ? decimal.Round(value.Value, 2) : null;
    }

    private static string? ReadString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value)
            ? value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number => value.GetRawText(),
                _ => null
            }
            : null;

    private static decimal? ReadDecimal(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetDecimal(out var number) => number,
            JsonValueKind.String when decimal.TryParse(value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null
        };
    }

    private static long? ReadLong(JsonElement element, string property)
    {
        var value = ReadDecimal(element, property);
        return value is null ? null : (long)decimal.Truncate(value.Value);
    }
}
