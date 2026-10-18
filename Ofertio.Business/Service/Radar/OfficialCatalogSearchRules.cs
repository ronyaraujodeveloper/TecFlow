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
        "API MLB recusou a busca (HTTP 403) e a vitrine pública do Mercado Livre não retornou ofertas. Conecte a loja via OAuth ou tente novamente em instantes.";

    public const string MercadoLivreScraperBlockedMessage =
        "A vitrine pública do Mercado Livre pediu verificação anti-robô e não retornou ofertas. Tente novamente em instantes.";

    /// <summary>401/403 ou erro de escopo de usuário: a busca deve cair na vitrine HTML.</summary>
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

    public static string BuildMercadoLivreListaUrl(string query)
    {
        var slug = string.Join(
            "-",
            query.Trim().Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));
        return "https://lista.mercadolivre.com.br/" + Uri.EscapeDataString(slug);
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
        OfficialCatalogSearchFilter filter)
    {
        if (filter.MinPrice is > 0 && (item.Price is null || item.Price < filter.MinPrice))
        {
            return false;
        }

        if (filter.MaxPrice is > 0 && (item.Price is null || item.Price > filter.MaxPrice))
        {
            return false;
        }

        return filter.HasCoupon != true || !string.IsNullOrWhiteSpace(item.CouponCode);
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
