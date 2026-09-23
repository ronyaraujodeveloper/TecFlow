using System.Text.Json;
using System.Text.Json.Serialization;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Dto;

/// <summary>Aceita enum numérico, string ("Shopee") ou 0/vazio como Shopee.</summary>
public sealed class MarketplaceTypeJsonConverter : JsonConverter<MarketplaceType>
{
    public override MarketplaceType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var numeric))
        {
            if (numeric == 0)
            {
                return MarketplaceType.Shopee;
            }

            if (Enum.IsDefined(typeof(MarketplaceType), numeric))
            {
                return (MarketplaceType)numeric;
            }
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var raw = reader.GetString()?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(raw) || raw == "0")
            {
                return MarketplaceType.Shopee;
            }

            if (Enum.TryParse(raw, ignoreCase: true, out MarketplaceType parsed))
            {
                return parsed;
            }

            if (raw.Contains("kabum", StringComparison.OrdinalIgnoreCase))
            {
                return MarketplaceType.Kabum;
            }

            if (raw.Contains("casasbahia", StringComparison.OrdinalIgnoreCase)
                || raw.Contains("casas bahia", StringComparison.OrdinalIgnoreCase))
            {
                return MarketplaceType.CasasBahia;
            }

            if (raw.Contains("amazon", StringComparison.OrdinalIgnoreCase)
                || raw.Contains("amzn", StringComparison.OrdinalIgnoreCase))
            {
                return MarketplaceType.Amazon;
            }

            if (raw.Contains("magalu", StringComparison.OrdinalIgnoreCase)
                || raw.Contains("magazine", StringComparison.OrdinalIgnoreCase))
            {
                return MarketplaceType.MagazineLuiza;
            }

            if (raw.Contains("mercado", StringComparison.OrdinalIgnoreCase)
                || raw.Contains("mlb", StringComparison.OrdinalIgnoreCase))
            {
                return MarketplaceType.MercadoLivre;
            }

            if (raw.Contains("tiktok", StringComparison.OrdinalIgnoreCase))
            {
                return MarketplaceType.TikTokShop;
            }

            if (raw.Contains("shopee", StringComparison.OrdinalIgnoreCase))
            {
                return MarketplaceType.Shopee;
            }
        }

        if (reader.TokenType is JsonTokenType.Null or JsonTokenType.None)
        {
            return MarketplaceType.Shopee;
        }

        return MarketplaceType.Shopee;
    }

    public override void Write(Utf8JsonWriter writer, MarketplaceType value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
