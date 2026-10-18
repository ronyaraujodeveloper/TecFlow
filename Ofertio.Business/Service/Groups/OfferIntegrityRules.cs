using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Ofertio.Core.Entities;

namespace Ofertio.Business.Service.Groups;

public static class OfferIntegrityRules
{
    public const decimal SuspiciousPriceThreshold = 5.00m;

    public static readonly string[] GenericStorefrontPathSegments =
    [
        "/social/",
        "/lists/",
        "/perfil/",
        "/perfil-vendedor/"
    ];

    public static readonly string[] ClosedListingPhrases =
    [
        "Anúncio pausado",
        "Publicação pausada",
        "Anúncio finalizado",
        "Publicação finalizada",
        "Anúncio encerrado",
        "Este anúncio foi finalizado",
        "Esta publicação foi finalizada",
        "Este anúncio está pausado",
        "Esta publicação está pausada"
    ];

    private static readonly string[] HighValueKeywords =
    [
        "tv", "smart tv", "televisao", "televisor",
        "notebook", "macbook", "laptop", "ultrabook", "chromebook",
        "smartphone", "celular", "iphone", "galaxy", "motorola edge", "xiaomi redmi",
        "monitor", "tablet", "ipad",
        "playstation", "ps5", "ps4", "xbox", "nintendo switch", "console",
        "geladeira", "refrigerador", "ar condicionado", "ar-condicionado",
        "lavadora", "maquina de lavar", "fogao", "micro-ondas", "microondas"
    ];

    private static readonly string[] AccessoryKeywords =
    [
        "capa", "capinha", "case", "pelicula", "cabo", "suporte", "carregador",
        "adaptador", "protetor", "adesivo", "chaveiro", "controle remoto", "skin",
        "filtro", "borracha", "parafuso", "pulseira", "caneta"
    ];

    private static readonly Regex HighValueRegex = BuildWordRegex(HighValueKeywords);
    private static readonly Regex AccessoryRegex = BuildWordRegex(AccessoryKeywords);

    public static bool IsGenericStorefrontUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)
            || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        var path = uri.AbsolutePath.ToLowerInvariant();
        if (!path.EndsWith('/'))
        {
            path += "/";
        }

        foreach (var segment in GenericStorefrontPathSegments)
        {
            if (path.Contains(segment, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static bool ContainsClosedListingPhrase(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return false;
        }

        foreach (var phrase in ClosedListingPhrases)
        {
            if (html.Contains(phrase, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsHighValueProduct(string? productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            return false;
        }

        var normalized = Normalize(productName);
        return HighValueRegex.IsMatch(normalized) && !AccessoryRegex.IsMatch(normalized);
    }

    public static bool IsSuspiciousPrice(string? productName, decimal? price) =>
        price is > 0 and < SuspiciousPriceThreshold && IsHighValueProduct(productName);

    public static string ApplyPriceSanity(string status, string? productName, decimal? price) =>
        GroupOfferStatuses.IsRevalidatable(status) && IsSuspiciousPrice(productName, price)
            ? GroupOfferStatuses.PrecoSuspeito
            : status;

    private static Regex BuildWordRegex(IEnumerable<string> keywords)
    {
        var pattern = string.Join("|", keywords.Select(Regex.Escape));
        return new Regex($@"(?<![\p{{L}}\p{{N}}])(?:{pattern})(?![\p{{L}}\p{{N}}])", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }

    private static string Normalize(string value)
    {
        var decomposed = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
