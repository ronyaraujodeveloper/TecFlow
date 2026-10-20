using System.Text.Json;
using Ofertio.Business.Dto;
using Ofertio.Business.Integrations.MercadoLivre;
using Ofertio.Business.Service.Radar;
using Ofertio.Core.Enums;

namespace Ofertio.Infrastructure.Services.Radar;

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
    public static IReadOnlyList<string> ParseCatalogSearchIds(string json)
    {
        var ids = new List<string>();
        if (string.IsNullOrWhiteSpace(json))
        {
            return ids;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object
                || !doc.RootElement.TryGetProperty("results", out var results)
                || results.ValueKind != JsonValueKind.Array)
            {
                return ids;
            }

            foreach (var product in results.EnumerateArray())
            {
                var id = ReadString(product, "id");
                if (id is not null && !ids.Contains(id, StringComparer.OrdinalIgnoreCase))
                {
                    ids.Add(id);
                }
            }
        }
        catch (JsonException)
        {
        }

        return ids;
    }

    /// <summary>
    /// <c>/products/{id}</c> como card da Buscar Ofertas: preço e frete do anúncio ganhador da buy box.
    /// Sem <c>buy_box_winner</c> não há oferta ativa para divulgar.
    /// </summary>
    public static OfficialCatalogProductDto? ParseCatalogOffer(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || root.TryGetProperty("error", out _)
                || !root.TryGetProperty("buy_box_winner", out var winner)
                || winner.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var price = ReadDecimal(winner, "price");
            var id = ReadString(root, "id");
            if (price is null or <= 0 || id is null)
            {
                return null;
            }

            var (_, name, image) = ParseCatalogProduct(json);
            var free = winner.TryGetProperty("shipping", out var shipping)
                && shipping.ValueKind == JsonValueKind.Object
                && shipping.TryGetProperty("free_shipping", out var freeEl)
                && freeEl.ValueKind == JsonValueKind.True;
            return new OfficialCatalogProductDto
            {
                Platform = nameof(MarketplaceType.MercadoLivre),
                PlatformType = MarketplaceType.MercadoLivre,
                ProductId = id,
                ProductName = string.IsNullOrWhiteSpace(name) ? null : name,
                Price = price,
                OriginalPrice = ReadDecimal(winner, "original_price"),
                ImageUrl = image?.Replace("http://", "https://", StringComparison.OrdinalIgnoreCase),
                SourceUrl = ReadString(root, "permalink") ?? "https://www.mercadolivre.com.br/p/" + id,
                Shipping = free ? "Frete grátis" : null,
                Source = "Api"
            };
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
        if (!root.TryGetProperty(name, out var el) || el.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return el.TryGetDecimal(out var value) ? value : null;
    }
}
