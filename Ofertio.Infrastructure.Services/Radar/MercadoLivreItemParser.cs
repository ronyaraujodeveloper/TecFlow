using System.Text.Json;
using Ofertio.Business.Dto;
using Ofertio.Business.Integrations.MercadoLivre;
using Ofertio.Business.Service.Radar;
using Ofertio.Core.Enums;

namespace Ofertio.Infrastructure.Services.Radar;

public sealed record MercadoLivreCatalogSearchPage(
    IReadOnlyList<string> ProductIds,
    int? Total,
    IReadOnlyList<string> DomainIds);

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
        string? image = null;
        if (root.TryGetProperty("pictures", out var pictures) && pictures.ValueKind == JsonValueKind.Array)
        {
            foreach (var picture in pictures.EnumerateArray())
            {
                if (picture.ValueKind == JsonValueKind.Object
                    && picture.TryGetProperty("url", out var urlEl)
                    && urlEl.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(urlEl.GetString()))
                {
                    image = urlEl.GetString();
                    break;
                }
            }
        }

        return (string.IsNullOrWhiteSpace(winnerId) ? null : winnerId.Trim(), name, image);
    }

    /// <summary><c>/products/search</c>: códigos dos produtos de catálogo, na ordem de relevância do Mercado Livre.</summary>
    public static IReadOnlyList<string> ParseCatalogSearchIds(string json) => ParseCatalogSearchPage(json).ProductIds;

    /// <summary>Página do <c>/products/search</c>: ids (ordem de relevância), total informado em <c>paging</c> e domínios.</summary>
    public static MercadoLivreCatalogSearchPage ParseCatalogSearchPage(string json)
    {
        var ids = new List<string>();
        var domains = new List<string>();
        int? total = null;
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "null" : json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return new MercadoLivreCatalogSearchPage(ids, total, domains);
            }

            if (root.TryGetProperty("paging", out var paging)
                && paging.ValueKind == JsonValueKind.Object
                && paging.TryGetProperty("total", out var totalEl)
                && totalEl.TryGetInt32(out var parsedTotal))
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
                    }
                }
            }
        }
        catch (JsonException)
        {
        }

        return new MercadoLivreCatalogSearchPage(ids, total, domains);
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
        var free = offer.TryGetProperty("shipping", out var shipping)
            && shipping.ValueKind == JsonValueKind.Object
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
            SourceUrl = ReadString(product, "permalink") ?? "https://www.mercadolivre.com.br/p/" + id,
            Shipping = free ? "Frete grátis" : null,
            Source = "Api",
            DomainId = ReadString(product, "domain_id")
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

        return el.TryGetDecimal(out var value) ? value : null;
    }
}
