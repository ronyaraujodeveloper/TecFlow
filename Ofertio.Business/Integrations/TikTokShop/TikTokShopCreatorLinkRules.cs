using System.Text.Json;
using Ofertio.Business.Integrations.Auth;

namespace Ofertio.Business.Integrations.TikTokShop;

/// <summary>
/// Link de promoção oficial (Affiliate Creator) gerado com o token de criador do próprio cliente, conectado em
/// Minhas Lojas. Sem esse token, o link segue com <c>sub_id</c> e o cliente é avisado de que a comissão não é garantida.
/// </summary>
public static class TikTokShopCreatorLinkRules
{
    public const string NoCreatorAccountWarning =
        "Conecte sua conta de criador TikTok em Minhas Lojas para garantir a comissão.";

    public const string OfficialLinkFailedWarning =
        "O TikTok Shop não gerou o link oficial de criador; usamos o link do produto com o seu ID. Reconecte a conta de criador em Minhas Lojas se o problema continuar.";

    public const string ProductIdPlaceholder = "{product_id}";

    /// <summary>Token real de criador (o Universal Link sem OAuth guarda um token de homologação).</summary>
    public static bool HasCreatorToken(string? accessToken) =>
        !string.IsNullOrWhiteSpace(accessToken) && !HomologMarketplaceAuth.IsStubToken(accessToken);

    public static string BuildPromotionLinkPath(string template, string productId)
    {
        var path = (template ?? string.Empty).Trim().TrimStart('/');
        return "/" + path.Replace(ProductIdPlaceholder, Uri.EscapeDataString(productId.Trim()), StringComparison.Ordinal);
    }

    /// <summary>Lê o link do envelope <c>{code, message, data}</c>; <paramref name="errorCode"/> é nulo quando <c>code</c> = 0.</summary>
    public static string? TryExtractPromotionLink(string? json, out long? errorCode, out string? errorMessage)
    {
        errorCode = null;
        errorMessage = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (root.TryGetProperty("code", out var code) && code.ValueKind == JsonValueKind.Number && code.TryGetInt64(out var value) && value != 0)
            {
                errorCode = value;
                errorMessage = root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String
                    ? message.GetString()
                    : null;
                return null;
            }

            if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            foreach (var name in new[] { "product_promotion_link", "promotion_link", "affiliate_link", "affiliateLink", "link", "url" })
            {
                if (!data.TryGetProperty(name, out var link))
                {
                    continue;
                }

                var candidate = link.ValueKind switch
                {
                    JsonValueKind.String => link.GetString(),
                    JsonValueKind.Object when link.TryGetProperty("url", out var nested) && nested.ValueKind == JsonValueKind.String => nested.GetString(),
                    _ => null
                };
                if (!string.IsNullOrWhiteSpace(candidate))
                {
                    return candidate.Trim();
                }
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
