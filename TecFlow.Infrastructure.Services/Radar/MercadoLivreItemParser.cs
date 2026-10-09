using System.Text.Json;
using TecFlow.Business.Dto;
using TecFlow.Business.Service.Radar;
using TecFlow.Core.Enums;

namespace TecFlow.Infrastructure.Services.Radar;

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
        var available = !string.Equals(status, "closed", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(status, "paused", StringComparison.OrdinalIgnoreCase);
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
            Status = LiveSearchRules.MapListingStatus(status, available),
            Price = price,
            OriginalPrice = original,
            ProductName = root.TryGetProperty("title", out var title) ? title.GetString() : null,
            ImageUrl = image,
            Shipping = free ? "Frete grátis" : null,
            Source = "Api",
            Platform = MarketplaceType.MercadoLivre
        };
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
