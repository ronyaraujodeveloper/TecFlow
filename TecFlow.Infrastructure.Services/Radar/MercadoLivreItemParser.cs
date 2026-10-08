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

    private static decimal? ReadDecimal(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el) || el.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return el.TryGetDecimal(out var value) ? value : null;
    }
}
