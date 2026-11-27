using Ofertio.Core.Enums;

namespace Ofertio.Business.Service.PublicPages;

/// <summary>
/// Frases da página pública sobre lojas aceitas. A tela (checagem antes de converter) e o servidor
/// (recusa na conversão) usam as mesmas frases, para o visitante ver sempre o mesmo texto.
/// </summary>
public static class PublicConverterStoreMessages
{
    public const string FallbackAffiliateName = "o afiliado desta página";

    public static string AffiliateName(string? displayName) =>
        string.IsNullOrWhiteSpace(displayName) ? FallbackAffiliateName : displayName.Trim();

    /// <summary>Nome da loja com o artigo certo: "da Shopee", "do Mercado Livre".</summary>
    public static string StoreReference(MarketplaceType platform)
    {
        var article = platform switch
        {
            MarketplaceType.MercadoLivre or MarketplaceType.TikTokShop or MarketplaceType.Kabum or MarketplaceType.AliExpress => "do",
            _ => "da"
        };
        return $"{article} {platform.GetDisplayName()}";
    }

    /// <summary>Site que o sistema não conhece pelo nome: "do site netshoes.com.br".</summary>
    public static string SiteReference(string? host)
    {
        var clean = (host ?? string.Empty).Trim().ToLowerInvariant();
        if (clean.StartsWith("www.", StringComparison.Ordinal))
        {
            clean = clean[4..];
        }

        return string.IsNullOrWhiteSpace(clean) ? "de uma loja" : $"do site {clean}";
    }

    public static string NotServed(string storeReference, string? displayName) =>
        $"Esse link é {storeReference}, que {AffiliateName(displayName)} ainda não atende. Use um link de uma das lojas abaixo.";

    public static string NotServed(MarketplaceType platform, string? displayName) =>
        NotServed(StoreReference(platform), displayName);

    /// <summary>Recusa de um endereço que não é de nenhuma loja conhecida, citando o site quando dá para ler.</summary>
    public static string NotServedSite(string? url, string? displayName) =>
        NotServed(SiteReference(TryReadHost(url)), displayName);

    public static string? TryReadHost(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var trimmed = url.Trim();
        if (!trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = "https://" + trimmed;
        }

        return Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) && uri.Host.Contains('.')
            ? uri.Host
            : null;
    }
}
