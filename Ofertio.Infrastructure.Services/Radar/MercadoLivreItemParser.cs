using System.Text.Json;
using Ofertio.Business.Dto;
using Ofertio.Business.Integrations.MercadoLivre;
using Ofertio.Business.Service.Radar;
using Ofertio.Core.Enums;

namespace Ofertio.Infrastructure.Services.Radar;

public sealed record MercadoLivreCatalogSearchPage(
    IReadOnlyList<string> ProductIds,
    int? Total,
    IReadOnlyList<string> DomainIds,
    IReadOnlyDictionary<string, string> DomainByProductId);

public sealed record MercadoLivreListingMetrics(
    int? SoldQuantity,
    string? SellerId,
    bool IsOfficialStore,
    bool IsFulfillment,
    string? Warranty,
    string? Condition,
    string? CategoryId);

public sealed record MercadoLivreSellerMetrics(
    string? Nickname,
    string? ReputationLevel,
    string? PowerSellerStatus,
    int? CompletedSales,
    decimal? PositiveRate);

public sealed record MercadoLivreReviewSummary(decimal? Average, int Total);

public sealed record MercadoLivreCategoryPath(string? Name, string? RootId, string? RootName);

public static class MercadoLivreItemParser
{
    public static OfficialOfferSnapshotDto? Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.TryGetProperty("error", out _))
        {
            return null;
        }

        var status = root.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : null;
        var quantity = root.TryGetProperty("available_quantity", out var quantityEl) && quantityEl.TryGetInt32(out var parsedQuantity)
            ? parsedQuantity
            : (int?)null;
        var unavailableStatus = MercadoLivreAvailabilityRules.FromApiItem(status, quantity);
        var available = unavailableStatus is null;
        decimal? price = ReadDecimal(root, "price");
        decimal? original = ReadDecimal(root, "original_price");
        var free = root.TryGetProperty("shipping", out var shipping)
            && shipping.TryGetProperty("free_shipping", out var freeEl)
            && freeEl.ValueKind == JsonValueKind.True;
        var image = root.TryGetProperty("secure_thumbnail", out var thumb) ? thumb.GetString() : null;
        image ??= root.TryGetProperty("thumbnail", out var thumb2) ? thumb2.GetString() : null;

        return new OfficialOfferSnapshotDto
        {
            IsAvailable = available,
            Status = unavailableStatus ?? LiveSearchRules.MapListingStatus(status, available),
            Price = price,
            OriginalPrice = original,
            ProductName = root.TryGetProperty("title", out var title) ? title.GetString() : null,
            ImageUrl = image,
            Shipping = free ? "Frete grátis" : null,
            Source = "Api",
            Platform = MarketplaceType.MercadoLivre
        };
    }

    /// <summary>Produto de catálogo (<c>/products/{id}</c>, links <c>/p/MLB...</c>): anúncio ganhador da buy box, nome e foto.</summary>
    public static (string? WinnerItemId, string? Name, string? ImageUrl) ParseCatalogProduct(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return (null, null, null);
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object || root.TryGetProperty("error", out _))
        {
            return (null, null, null);
        }

        var winnerId = root.TryGetProperty("buy_box_winner", out var winner)
            && winner.ValueKind == JsonValueKind.Object
            && winner.TryGetProperty("item_id", out var winnerIdEl)
            && winnerIdEl.ValueKind == JsonValueKind.String
                ? winnerIdEl.GetString()
                : null;
        var name = root.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String
            ? nameEl.GetString()
            : null;
        var image = ReadPictureUrls(root, 1).FirstOrDefault();

        return (string.IsNullOrWhiteSpace(winnerId) ? null : winnerId.Trim(), name, image);
    }

    /// <summary>
    /// Metadados do produto de catálogo para o Gerador de Links. Nome e foto vêm do produto;
    /// o preço prefere o anúncio ganhador e, sem ele, o mínimo da faixa. Nulo só quando não há nome, preço nem foto.
    /// </summary>
    public static OfficialOfferSnapshotDto? ParseCatalogSnapshot(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "null" : json);
            var root = doc.RootElement;
            if (!IsProduct(root))
            {
                return null;
            }

            var (_, name, image) = ParseCatalogProduct(root.GetRawText());
            decimal? price = null;
            decimal? original = null;
            string? shipping = null;
            if (root.TryGetProperty("buy_box_winner", out var winner) && winner.ValueKind == JsonValueKind.Object)
            {
                price = ReadDecimal(winner, "price");
                original = ReadDecimal(winner, "original_price");
                if (winner.TryGetProperty("shipping", out var ship)
                    && ship.ValueKind == JsonValueKind.Object
                    && ship.TryGetProperty("free_shipping", out var free)
                    && free.ValueKind == JsonValueKind.True)
                {
                    shipping = "Frete grátis";
                }
            }

            if (price is null or <= 0
                && root.TryGetProperty("buy_box_winner_price_range", out var range)
                && range.ValueKind == JsonValueKind.Object
                && range.TryGetProperty("min", out var min))
            {
                price = ReadDecimal(min, "price");
            }

            if (string.IsNullOrWhiteSpace(name) && price is null or <= 0 && string.IsNullOrWhiteSpace(image))
            {
                return null;
            }

            return new OfficialOfferSnapshotDto
            {
                IsAvailable = true,
                Price = price is > 0 ? price : null,
                OriginalPrice = original > price ? original : null,
                ProductName = string.IsNullOrWhiteSpace(name) ? null : name.Trim(),
                ImageUrl = string.IsNullOrWhiteSpace(image) ? null : image,
                Shipping = shipping,
                Source = "Api",
                Platform = MarketplaceType.MercadoLivre
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public const int MaxGalleryPictures = 12;

    /// <summary><c>pictures[].url</c> do produto de catálogo, sem repetição, em https.</summary>
    private static List<string> ReadPictureUrls(JsonElement product, int max)
    {
        var urls = new List<string>();
        if (!product.TryGetProperty("pictures", out var pictures) || pictures.ValueKind != JsonValueKind.Array)
        {
            return urls;
        }

        foreach (var picture in pictures.EnumerateArray())
        {
            var url = ReadString(picture, "url") ?? ReadString(picture, "secure_url");
            if (url is null)
            {
                continue;
            }

            url = url.Replace("http://", "https://", StringComparison.OrdinalIgnoreCase);
            if (!urls.Contains(url, StringComparer.OrdinalIgnoreCase))
            {
                urls.Add(url);
            }

            if (urls.Count >= max)
            {
                break;
            }
        }

        return urls;
    }

    /// <summary><c>/products/search</c>: códigos dos produtos de catálogo, na ordem de relevância do Mercado Livre.</summary>
    public static IReadOnlyList<string> ParseCatalogSearchIds(string json) => ParseCatalogSearchPage(json).ProductIds;

    /// <summary>Página do <c>/products/search</c>: ids (ordem de relevância), total informado em <c>paging</c> e domínios.</summary>
    public static MercadoLivreCatalogSearchPage ParseCatalogSearchPage(string json)
    {
        var ids = new List<string>();
        var domains = new List<string>();
        var domainByProduct = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        int? total = null;
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "null" : json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return new MercadoLivreCatalogSearchPage(ids, total, domains, domainByProduct);
            }

            if (root.TryGetProperty("paging", out var paging)
                && paging.ValueKind == JsonValueKind.Object
                && paging.TryGetProperty("total", out var totalEl)
                && TryReadInt(totalEl, out var parsedTotal))
            {
                total = parsedTotal;
            }

            if (root.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array)
            {
                foreach (var product in results.EnumerateArray())
                {
                    var id = ReadString(product, "id");
                    if (id is not null && !ids.Contains(id, StringComparer.OrdinalIgnoreCase))
                    {
                        ids.Add(id);
                    }

                    var domain = ReadString(product, "domain_id");
                    if (domain is not null)
                    {
                        domains.Add(domain);
                        if (id is not null)
                        {
                            domainByProduct.TryAdd(id, domain);
                        }
                    }
                }
            }
        }
        catch (JsonException)
        {
        }

        return new MercadoLivreCatalogSearchPage(ids, total, domains, domainByProduct);
    }

    /// <summary><c>/sites/MLB/domain_discovery/search</c>: domínios de catálogo prováveis para o termo, com nome.</summary>
    public static IReadOnlyList<OfficialCatalogCategoryDto> ParseDomainDiscovery(string json)
    {
        var categories = new List<OfficialCatalogCategoryDto>();
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "null" : json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return categories;
            }

            foreach (var entry in doc.RootElement.EnumerateArray())
            {
                var id = ReadString(entry, "domain_id");
                if (id is null || categories.Any(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                categories.Add(new OfficialCatalogCategoryDto
                {
                    Id = id,
                    Name = ReadString(entry, "domain_name") ?? ReadString(entry, "category_name") ?? id
                });
            }
        }
        catch (JsonException)
        {
        }

        return categories;
    }

    /// <summary>
    /// <c>/products/{id}</c> como card da Buscar Ofertas: preço e frete do anúncio ganhador da buy box ou, sem
    /// disputa, o menor preço da faixa (<c>buy_box_winner_price_range.min</c>). Sem nenhum dos dois retorna nulo.
    /// </summary>
    public static OfficialCatalogProductDto? ParseCatalogOffer(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "null" : json);
            var root = doc.RootElement;
            if (!IsProduct(root))
            {
                return null;
            }

            if (root.TryGetProperty("buy_box_winner", out var winner) && winner.ValueKind == JsonValueKind.Object)
            {
                var offer = BuildCatalogOffer(root, winner);
                if (offer is not null)
                {
                    return offer;
                }
            }

            return root.TryGetProperty("buy_box_winner_price_range", out var range)
                && range.ValueKind == JsonValueKind.Object
                && range.TryGetProperty("min", out var min)
                    ? BuildCatalogOffer(root, min)
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Produto sem buy box: o menor preço entre os anúncios de <c>/products/{id}/items</c>, com nome, foto e página
    /// vindos do próprio produto de catálogo.
    /// </summary>
    public static OfficialCatalogProductDto? ParseCatalogItemsOffer(string productJson, string itemsJson)
    {
        try
        {
            using var product = JsonDocument.Parse(string.IsNullOrWhiteSpace(productJson) ? "null" : productJson);
            using var items = JsonDocument.Parse(string.IsNullOrWhiteSpace(itemsJson) ? "null" : itemsJson);
            if (!IsProduct(product.RootElement)
                || items.RootElement.ValueKind != JsonValueKind.Object
                || !items.RootElement.TryGetProperty("results", out var results)
                || results.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            JsonElement? cheapest = null;
            decimal? cheapestPrice = null;
            foreach (var item in results.EnumerateArray())
            {
                var price = item.ValueKind == JsonValueKind.Object ? ReadDecimal(item, "price") : null;
                if (price is > 0 && (cheapestPrice is null || price < cheapestPrice))
                {
                    cheapest = item;
                    cheapestPrice = price;
                }
            }

            return cheapest is null ? null : BuildCatalogOffer(product.RootElement, cheapest.Value);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Produto "pai" (família sem configuração definida) não é comprável; as ofertas ficam nos filhos.</summary>
    public static IReadOnlyList<string> ParseCatalogChildrenIds(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "null" : json);
            if (!IsProduct(doc.RootElement)
                || !doc.RootElement.TryGetProperty("children_ids", out var children)
                || children.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return children.EnumerateArray()
                .Where(child => child.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(child.GetString()))
                .Select(child => child.GetString()!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static bool IsProduct(JsonElement root) =>
        root.ValueKind == JsonValueKind.Object
        && !root.TryGetProperty("error", out _)
        && ReadString(root, "id") is not null;

    private static OfficialCatalogProductDto? BuildCatalogOffer(JsonElement product, JsonElement offer)
    {
        var price = ReadDecimal(offer, "price");
        if (price is null or <= 0)
        {
            return null;
        }

        var id = ReadString(product, "id")!;
        var (_, name, image) = ParseCatalogProduct(product.GetRawText());
        var hasShipping = offer.TryGetProperty("shipping", out var shipping) && shipping.ValueKind == JsonValueKind.Object;
        var free = hasShipping
            && shipping.TryGetProperty("free_shipping", out var freeEl)
            && freeEl.ValueKind == JsonValueKind.True;
        var original = ReadDecimal(offer, "original_price");
        return new OfficialCatalogProductDto
        {
            Platform = nameof(MarketplaceType.MercadoLivre),
            PlatformType = MarketplaceType.MercadoLivre,
            ProductId = id,
            ProductName = string.IsNullOrWhiteSpace(name) ? null : name,
            Price = price,
            OriginalPrice = original > price ? original : null,
            ImageUrl = image?.Replace("http://", "https://", StringComparison.OrdinalIgnoreCase),
            ImageUrls = ReadPictureUrls(product, MaxGalleryPictures),
            SourceUrl = ReadString(product, "permalink") ?? MercadoLivreCommissionUrlBuilder.BuildDirectProductUrl(id),
            Shipping = free ? "Frete grátis" : null,
            Source = "Api",
            DomainId = ReadString(product, "domain_id"),
            ListingId = ReadString(offer, "item_id"),
            CategoryId = ReadString(offer, "category_id"),
            SellerId = ReadId(offer, "seller_id"),
            IsOfficialStore = ReadId(offer, "official_store_id") is not null,
            IsFulfillment = hasShipping && string.Equals(ReadString(shipping, "logistic_type"), "fulfillment", StringComparison.OrdinalIgnoreCase),
            Warranty = ReadString(offer, "warranty"),
            Condition = ReadString(offer, "condition")
        };
    }

    /// <summary><c>/items?ids=...&amp;attributes=...</c>: dados de anúncio por id (vendas só quando o ML informa).</summary>
    public static IReadOnlyDictionary<string, MercadoLivreListingMetrics> ParseItemsMultiget(string json)
    {
        var result = new Dictionary<string, MercadoLivreListingMetrics>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "null" : json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return result;
            }

            foreach (var entry in doc.RootElement.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object
                    || (entry.TryGetProperty("code", out var code) && TryReadInt(code, out var status) && status != 200)
                    || !entry.TryGetProperty("body", out var body)
                    || ReadString(body, "id") is not { } id)
                {
                    continue;
                }

                var hasShipping = body.TryGetProperty("shipping", out var shipping) && shipping.ValueKind == JsonValueKind.Object;
                int? sold = body.TryGetProperty("sold_quantity", out var soldEl) && TryReadInt(soldEl, out var soldValue) && soldValue > 0
                    ? soldValue
                    : null;
                result[id] = new MercadoLivreListingMetrics(
                    sold,
                    ReadId(body, "seller_id"),
                    ReadId(body, "official_store_id") is not null,
                    hasShipping && string.Equals(ReadString(shipping, "logistic_type"), "fulfillment", StringComparison.OrdinalIgnoreCase),
                    ReadString(body, "warranty"),
                    ReadString(body, "condition"),
                    ReadString(body, "category_id"));
            }
        }
        catch (JsonException)
        {
        }

        return result;
    }

    /// <summary><c>/users/{id}</c>: apelido e reputação pública do vendedor.</summary>
    public static MercadoLivreSellerMetrics? ParseSeller(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "null" : json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("seller_reputation", out var reputation)
                || reputation.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            int? completed = null;
            decimal? positive = null;
            if (reputation.TryGetProperty("transactions", out var transactions) && transactions.ValueKind == JsonValueKind.Object)
            {
                if (transactions.TryGetProperty("completed", out var completedEl) && TryReadInt(completedEl, out var completedValue))
                {
                    completed = completedValue;
                }

                if (transactions.TryGetProperty("ratings", out var ratings) && ratings.ValueKind == JsonValueKind.Object)
                {
                    positive = ReadDecimal(ratings, "positive");
                }
            }

            return new MercadoLivreSellerMetrics(
                ReadString(root, "nickname"),
                ReadString(reputation, "level_id"),
                ReadString(reputation, "power_seller_status"),
                completed,
                positive);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary><c>/reviews/item/{id}</c>: nota média e total de avaliações.</summary>
    public static MercadoLivreReviewSummary? ParseReviewSummary(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "null" : json);
            var root = doc.RootElement;
            var average = ReadDecimal(root, "rating_average");
            int? total = root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("paging", out var paging)
                && paging.ValueKind == JsonValueKind.Object
                && paging.TryGetProperty("total", out var totalEl)
                && TryReadInt(totalEl, out var totalValue)
                    ? totalValue
                    : null;
            return average is > 0 || total is > 0
                ? new MercadoLivreReviewSummary(average is > 0 ? decimal.Round(average.Value, 1) : null, total ?? 0)
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary><c>/categories/{id}</c>: nome da categoria e a categoria raiz (primeiro item de <c>path_from_root</c>).</summary>
    public static MercadoLivreCategoryPath? ParseCategory(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "null" : json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object || ReadString(root, "id") is not { } id)
            {
                return null;
            }

            var name = ReadString(root, "name");
            if (root.TryGetProperty("path_from_root", out var path)
                && path.ValueKind == JsonValueKind.Array
                && path.GetArrayLength() > 0
                && ReadString(path[0], "id") is { } rootId)
            {
                return new MercadoLivreCategoryPath(name, rootId, ReadString(path[0], "name"));
            }

            return new MercadoLivreCategoryPath(name, id, name);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary><c>/highlights/MLB/category/{id}</c>: posição de produtos e anúncios no ranking de mais vendidos.</summary>
    public static IReadOnlyDictionary<string, int> ParseBestSellerPositions(string json)
    {
        var positions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "null" : json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object
                || !doc.RootElement.TryGetProperty("content", out var content)
                || content.ValueKind != JsonValueKind.Array)
            {
                return positions;
            }

            foreach (var entry in content.EnumerateArray())
            {
                if (ReadString(entry, "id") is { } id
                    && entry.TryGetProperty("position", out var positionEl)
                    && TryReadInt(positionEl, out var position)
                    && position > 0)
                {
                    positions.TryAdd(id, position);
                }
            }
        }
        catch (JsonException)
        {
        }

        return positions;
    }

    private static string? ReadId(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.String when !string.IsNullOrWhiteSpace(value.GetString()) => value.GetString()!.Trim(),
            _ => null
        };
    }

    public static string? ParseCatalogDomainId(string productJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(productJson) ? "null" : productJson);
            return ReadString(doc.RootElement, "domain_id");
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ReadString(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
        && !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()!.Trim()
            : null;

    private static decimal? ReadDecimal(JsonElement root, string name)
    {
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty(name, out var el)
            || el.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return el.ValueKind == JsonValueKind.Number && el.TryGetDecimal(out var value) ? value : null;
    }

    private static bool TryReadInt(JsonElement element, out int value)
    {
        value = 0;
        return element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out value);
    }
}
