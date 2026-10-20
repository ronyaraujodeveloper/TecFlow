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

    public static IReadOnlyList<OfficialCatalogProductDto> ParseSearch(string json)
    {
        var list = new List<OfficialCatalogProductDto>();
        if (string.IsNullOrWhiteSpace(json))
        {
            return list;
        }

        MercadoLivreSearchResponse? payload;
        try
        {
            payload = JsonSerializer.Deserialize<MercadoLivreSearchResponse>(json, SearchJsonOptions);
        }
        catch (JsonException)
        {
            return list;
        }

        foreach (var item in payload?.Results ?? [])
        {
            var id = item.Id;
            var permalink = item.Permalink;
            if (string.IsNullOrWhiteSpace(permalink) && !string.IsNullOrWhiteSpace(id))
            {
                permalink = "https://produto.mercadolivre.com.br/" + id.Replace("MLB", "MLB-", StringComparison.OrdinalIgnoreCase);
            }

            if (string.IsNullOrWhiteSpace(permalink))
            {
                continue;
            }

            var image = string.IsNullOrWhiteSpace(item.SecureThumbnail) ? item.Thumbnail : item.SecureThumbnail;
            list.Add(new OfficialCatalogProductDto
            {
                Platform = nameof(MarketplaceType.MercadoLivre),
                PlatformType = MarketplaceType.MercadoLivre,
                ProductId = string.IsNullOrWhiteSpace(id) ? null : id,
                ProductName = string.IsNullOrWhiteSpace(item.Title) ? null : item.Title,
                Price = item.Price,
                OriginalPrice = item.OriginalPrice,
                ImageUrl = string.IsNullOrWhiteSpace(image) ? null : image,
                SourceUrl = permalink,
                Shipping = item.Shipping?.FreeShipping == true ? "Frete grátis" : null,
                Source = "Api"
            });
        }

        return list;
    }

    private static readonly JsonSerializerOptions SearchJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static decimal? ReadDecimal(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el) || el.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return el.TryGetDecimal(out var value) ? value : null;
    }
}
