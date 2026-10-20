using System.Net;
using Ofertio.Business.Dto;
using Ofertio.Core.Enums;
using Ofertio.Database.Filter;

namespace Ofertio.Business.Service.Radar;

public static class OfficialCatalogSearchRules
{
    public const int MinKeywordLength = 2;
    public const int DefaultLimit = 20;
    public const int MaxLimit = 20;

    public static bool IsValidQuery(string? keyword) =>
        !string.IsNullOrWhiteSpace(keyword) && keyword.Trim().Length >= MinKeywordLength;

    public static int ClampLimit(int limit) =>
        limit <= 0 ? DefaultLimit : Math.Clamp(limit, 1, MaxLimit);

    public static string EscapeGraphQl(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

    public static bool HasRealAffiliateCredentials(string? appId, string? secret)
    {
        if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(secret))
        {
            return false;
        }

        var id = appId.Trim();
        var key = secret.Trim();
        if (id.Equals("100000", StringComparison.Ordinal))
        {
            return false;
        }

        return !key.Contains("homolog", StringComparison.OrdinalIgnoreCase)
            && !key.Contains("sandbox", StringComparison.OrdinalIgnoreCase);
    }

    public const string ShopeeMissingApiKeyMessage = "Chave de API não configurada";

    public const string AmazonMissingPaApiMessage = "PA-API não configurada";

    public const string MercadoLivreMissingAccountMessage =
        "Requer OAuth do Mercado Livre para buscar ofertas";

    public const string MercadoLivreSearchForbiddenMessage =
        "A API oficial do Mercado Livre recusou a busca (HTTP 403). Conecte ou reconecte a conta Mercado Livre em Minhas Lojas (OAuth); a vitrine pública não é consultada para evitar o bloqueio anti-robô.";

    /// <summary>401/403 ou erro de escopo de usuário na busca oficial (o token precisa ser renovado ou a conta reconectada).</summary>
    public static bool IsMercadoLivreSearchRefused(HttpStatusCode statusCode, string? body)
    {
        if (statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return true;
        }

        return statusCode != HttpStatusCode.OK
            && !string.IsNullOrWhiteSpace(body)
            && (body.Contains("scope", StringComparison.OrdinalIgnoreCase)
                || body.Contains("forbidden", StringComparison.OrdinalIgnoreCase)
                || body.Contains("invalid_token", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Desafio anti-bot da vitrine (account-verification / login) em vez da lista de produtos.</summary>
    public static bool IsMercadoLivreVerificationPage(string? finalUrl, string? html)
    {
        if (!string.IsNullOrWhiteSpace(finalUrl)
            && (finalUrl.Contains("account-verification", StringComparison.OrdinalIgnoreCase)
                || finalUrl.Contains("/gz/", StringComparison.OrdinalIgnoreCase)
                || finalUrl.Contains("/jms/", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(html)
            && VerificationMarkers.Any(marker => html.Contains(marker, StringComparison.OrdinalIgnoreCase))
            && !html.Contains("ui-search-layout", StringComparison.OrdinalIgnoreCase)
            && !html.Contains("poly-card", StringComparison.OrdinalIgnoreCase);
    }

    private static readonly string[] VerificationMarkers =
        ["account-verification", "bot_challenge", "_bmstate", "micro-landing-container"];

    public static bool MatchesMercadoLivreAccount(
        MarketplaceType marketplaceType,
        string? friendlyName,
        string? shopName) =>
        marketplaceType == MarketplaceType.MercadoLivre
        || IsMercadoLivreLabel(friendlyName)
        || IsMercadoLivreLabel(shopName);

    public static bool IsMercadoLivreLabel(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return string.Equals(
            name.Replace(" ", string.Empty, StringComparison.Ordinal),
            "MercadoLivre",
            StringComparison.OrdinalIgnoreCase);
    }

    public static string? ResolveMercadoLivreAffiliateId(string? trackingId, string? affiliateTrackingId)
    {
        if (!string.IsNullOrWhiteSpace(trackingId))
        {
            return trackingId.Trim();
        }

        return string.IsNullOrWhiteSpace(affiliateTrackingId) ? null : affiliateTrackingId.Trim();
    }

    public static bool PassesOptionalFilters(
        OfficialCatalogProductDto item,
        OfficialCatalogSearchFilter filter) =>
        PassesPriceAndShipping(item, filter.MinPrice, filter.MaxPrice, filter.FreeShippingOnly == true)
        && (filter.HasCoupon != true || !string.IsNullOrWhiteSpace(item.CouponCode));

    /// <summary>Faixa de preço e frete grátis: usados também durante a varredura do Mercado Livre.</summary>
    public static bool PassesPriceAndShipping(
        OfficialCatalogProductDto item,
        decimal? minPrice,
        decimal? maxPrice,
        bool freeShippingOnly)
    {
        if (minPrice is > 0 && (item.Price is null || item.Price < minPrice))
        {
            return false;
        }

        if (maxPrice is > 0 && (item.Price is null || item.Price > maxPrice))
        {
            return false;
        }

        return !freeShippingOnly || IsFreeShipping(item);
    }

    public static bool IsFreeShipping(OfficialCatalogProductDto item) =>
        !string.IsNullOrWhiteSpace(item.Shipping)
        && (item.Shipping.Contains("grátis", StringComparison.OrdinalIgnoreCase)
            || item.Shipping.Contains("gratis", StringComparison.OrdinalIgnoreCase)
            || item.Shipping.Contains("free", StringComparison.OrdinalIgnoreCase));

    public const string SortRelevance = "relevancia";
    public const string SortLowestPrice = "menor_preco";
    public const string SortBiggestDiscount = "maior_desconto";

    public static IReadOnlyList<OfficialCatalogProductDto> ApplySort(
        IReadOnlyList<OfficialCatalogProductDto> items,
        string? sortBy) => sortBy?.Trim().ToLowerInvariant() switch
        {
            SortLowestPrice => items.OrderBy(item => item.Price ?? decimal.MaxValue).ToList(),
            SortBiggestDiscount => items
                .OrderByDescending(DiscountPercent)
                .ThenBy(item => item.Price ?? decimal.MaxValue)
                .ToList(),
            _ => items
        };

    public static decimal DiscountPercent(OfficialCatalogProductDto item) =>
        item.Price is > 0 && item.OriginalPrice > item.Price
            ? (item.OriginalPrice.Value - item.Price.Value) / item.OriginalPrice.Value * 100m
            : 0m;

    public const int MercadoLivreScanPageSize = 20;

    /// <summary>Teto de produtos de catálogo analisados por busca (cada um custa 1 a 4 chamadas à API).</summary>
    public const int MercadoLivreMaxScannedProducts = 100;

    /// <summary>Com termos mais curtos ainda por tentar, o termo atual não consome o teto inteiro.</summary>
    public const int MercadoLivreScanPerQueryWithFallback = 40;

    /// <summary>"Dell Inspiron i7 16GB" ➔ "Dell Inspiron i7" ➔ "Dell Inspiron" ➔ "Dell".</summary>
    public static IReadOnlyList<string> BuildShorterQueries(string? query)
    {
        var words = (query ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var shorter = new List<string>();
        for (var count = words.Length - 1; count >= 1; count--)
        {
            var candidate = string.Join(' ', words.Take(count));
            if (IsValidQuery(candidate))
            {
                shorter.Add(candidate);
            }
        }

        return shorter;
    }

    /// <summary><c>MLB-GAMING_HEADSETS</c> ➔ "Gaming headsets" (quando o ML não informa o nome do domínio).</summary>
    public static string HumanizeMercadoLivreDomain(string domainId)
    {
        var name = domainId.Trim();
        var dash = name.IndexOf('-', StringComparison.Ordinal);
        if (dash >= 0)
        {
            name = name[(dash + 1)..];
        }

        name = name.Replace('_', ' ').Trim().ToLowerInvariant();
        return name.Length == 0 ? domainId : char.ToUpperInvariant(name[0]) + name[1..];
    }

    /// <summary>Selo do Mercado Livre: quantos produtos de catálogo foram analisados e quantos tinham oferta ativa.</summary>
    public static string DescribeMercadoLivreScan(int offers, int scanned, string? effectiveQuery, string? typedQuery)
    {
        var message = $"{offers} {(offers == 1 ? "oferta" : "ofertas")} · {scanned} produtos analisados";
        if (scanned > 0 && offers == 0)
        {
            message += " (sem anúncio ativo)";
        }

        return !string.IsNullOrWhiteSpace(effectiveQuery)
            && !string.Equals(effectiveQuery.Trim(), typedQuery?.Trim(), StringComparison.OrdinalIgnoreCase)
                ? $"{message} · buscado como \"{effectiveQuery.Trim()}\""
                : message;
    }

    /// <summary>Ofertas do canal que realmente chegam à tela (filtros opcionais + URLs únicas).</summary>
    public static int CountVisibleOffers(
        IEnumerable<OfficialCatalogProductDto> items,
        OfficialCatalogSearchFilter filter) =>
        items
            .Where(item => !string.IsNullOrWhiteSpace(item.SourceUrl) && PassesOptionalFilters(item, filter))
            .Select(item => item.SourceUrl.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

    public static bool HasConnectedMercadoLivreStore(
        MarketplaceType marketplaceType,
        string? friendlyName,
        string? shopName,
        string? trackingId,
        string? affiliateTrackingId) =>
        MatchesMercadoLivreAccount(marketplaceType, friendlyName, shopName)
        && !string.IsNullOrWhiteSpace(ResolveMercadoLivreAffiliateId(trackingId, affiliateTrackingId));

    public static OfficialCatalogChannelStatusDto BuildChannelStatus(
        string key,
        string label,
        bool included,
        int productCount,
        bool missingCredentials,
        string missingMessage,
        string? errorMessage = null)
    {
        if (!included)
        {
            return new OfficialCatalogChannelStatusDto
            {
                Key = key,
                Label = label,
                Included = false,
                ProductCount = 0,
                State = "skipped",
                Message = "Não consultada"
            };
        }

        if (productCount > 0)
        {
            return new OfficialCatalogChannelStatusDto
            {
                Key = key,
                Label = label,
                Included = true,
                ProductCount = productCount,
                State = "ok",
                Message = $"{productCount} produtos"
            };
        }

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            return new OfficialCatalogChannelStatusDto
            {
                Key = key,
                Label = label,
                Included = true,
                ProductCount = 0,
                State = "error",
                Message = errorMessage.Trim()
            };
        }

        if (missingCredentials)
        {
            return new OfficialCatalogChannelStatusDto
            {
                Key = key,
                Label = label,
                Included = true,
                ProductCount = 0,
                State = "missing",
                Message = missingMessage
            };
        }

        return new OfficialCatalogChannelStatusDto
        {
            Key = key,
            Label = label,
            Included = true,
            ProductCount = 0,
            State = "empty",
            Message = "0 produtos"
        };
    }
}
