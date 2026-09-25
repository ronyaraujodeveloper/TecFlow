using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Dto;
using TecFlow.Business.Integrations.Common;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.LinkStrategies;

namespace TecFlow.Infrastructure.Services.LinkStrategies;

/// <summary>Expande encurtadores e extrai metadados via API JSON da Shopee (itemid/shopid) ou OpenGraph.</summary>
public sealed class ProductMetadataService : IProductMetadataService
{
    private const int MaxHtmlChars = 512_000;
    private const decimal ShopeePriceScale = 100_000_000m;
    private const string ShopeeItemApiUrl = "https://shopee.com.br/api/v4/item/get";
    private const string ShopeeImageCdn = "https://down-br.img.susercontent.com/file/";
    private const string ShopeeApiUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";

    private static readonly Regex ShopeeItemPathRegex = new(
        @"-i\.(\d+)\.(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly string[] InvalidTitles =
    [
        "opaanlp",
        "nsbo",
        "shopee",
        "shopee brasil",
        "verification",
        "captcha",
        "just a moment",
        "produto"
    ];

    private readonly IUrlExpansionService _urlExpansionService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProductMetadataService> _logger;

    public ProductMetadataService(
        IUrlExpansionService urlExpansionService,
        IHttpClientFactory httpClientFactory,
        ILogger<ProductMetadataService> logger)
    {
        _urlExpansionService = urlExpansionService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<ProductMetadataDto> ExtractAsync(
        string productUrl,
        CancellationToken cancellationToken = default)
    {
        var workingUrl = string.IsNullOrWhiteSpace(productUrl) ? string.Empty : productUrl.Trim();
        var resolvedUrl = workingUrl;

        try
        {
            if (!string.IsNullOrWhiteSpace(workingUrl))
            {
                resolvedUrl = await _urlExpansionService.ExpandUrlAsync(workingUrl, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Falha ao expandir URL para metadados do produto. Url={Url}",
                workingUrl);
            resolvedUrl = workingUrl;
        }

        resolvedUrl = UnwrapNestedDestinationUrl(resolvedUrl);

        try
        {
            if (string.IsNullOrWhiteSpace(resolvedUrl) || !Uri.TryCreate(resolvedUrl, UriKind.Absolute, out _))
            {
                return SanitizeMetadata(ProductMetadataHtmlParser.FromUrlFallback(workingUrl), workingUrl);
            }

            if (TryParseShopeeItemIds(resolvedUrl, out var shopId, out var itemId))
            {
                var fromApi = await TryExtractFromShopeeItemApiAsync(shopId, itemId, cancellationToken);
                if (fromApi is not null)
                {
                    return SanitizeMetadata(fromApi, resolvedUrl);
                }
            }

            var client = _httpClientFactory.CreateClient(IntegrationHttpClientNames.ProductMetadata);
            using var request = new HttpRequestMessage(HttpMethod.Get, resolvedUrl);
            ApplyAntiBotBrowserHeaders(request);
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Scraping de metadados recusado. Status={Status} Url={Url}",
                    (int)response.StatusCode,
                    resolvedUrl);
                return SanitizeMetadata(ProductMetadataHtmlParser.FromUrlFallback(resolvedUrl), resolvedUrl);
            }

            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            if (html.Length > MaxHtmlChars)
            {
                html = html[..MaxHtmlChars];
            }

            var parsed = ProductMetadataHtmlParser.Parse(html, resolvedUrl);
            return SanitizeMetadata(parsed, resolvedUrl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Falha ao extrair metadados do produto. Url={Url}",
                resolvedUrl);
            return SanitizeMetadata(ProductMetadataHtmlParser.FromUrlFallback(resolvedUrl), resolvedUrl);
        }
    }

    public static bool TryParseShopeeItemIds(string? url, out string shopId, out string itemId)
    {
        shopId = string.Empty;
        itemId = string.Empty;
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var pathMatch = ShopeeItemPathRegex.Match(url);
        if (pathMatch.Success)
        {
            shopId = pathMatch.Groups[1].Value;
            itemId = pathMatch.Groups[2].Value;
            return IsPositiveId(shopId) && IsPositiveId(itemId);
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        var query = uri.Query.TrimStart('?');
        if (string.IsNullOrWhiteSpace(query))
        {
            return false;
        }

        string? queryShop = null;
        string? queryItem = null;
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length != 2)
            {
                continue;
            }

            var key = HttpUtilityUrlDecode(parts[0]);
            var value = HttpUtilityUrlDecode(parts[1]);
            if (key.Equals("shopid", StringComparison.OrdinalIgnoreCase)
                || key.Equals("shop_id", StringComparison.OrdinalIgnoreCase))
            {
                queryShop = value;
            }
            else if (key.Equals("itemid", StringComparison.OrdinalIgnoreCase)
                || key.Equals("item_id", StringComparison.OrdinalIgnoreCase))
            {
                queryItem = value;
            }
        }

        if (IsPositiveId(queryShop) && IsPositiveId(queryItem))
        {
            shopId = queryShop!;
            itemId = queryItem!;
            return true;
        }

        return false;
    }

    public static decimal? ConvertShopeePrice(decimal raw)
    {
        if (raw <= 0)
        {
            return null;
        }

        var reais = raw >= 1_000_000m
            ? raw / ShopeePriceScale
            : raw;
        reais = decimal.Round(reais, 2, MidpointRounding.AwayFromZero);
        return reais > 0 ? reais : null;
    }

    public static string UnwrapNestedDestinationUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return url ?? string.Empty;
        }

        var query = uri.Query.TrimStart('?');
        if (string.IsNullOrWhiteSpace(query))
        {
            return uri.ToString();
        }

        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length != 2)
            {
                continue;
            }

            var key = HttpUtilityUrlDecode(parts[0]);
            if (!key.Equals("target", StringComparison.OrdinalIgnoreCase)
                && !key.Equals("redirect", StringComparison.OrdinalIgnoreCase)
                && !key.Equals("redirect_url", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var nested = HttpUtilityUrlDecode(parts[1]);
            if (Uri.TryCreate(nested, UriKind.Absolute, out var nestedUri)
                && (nestedUri.Scheme == Uri.UriSchemeHttp || nestedUri.Scheme == Uri.UriSchemeHttps))
            {
                return UnwrapNestedDestinationUrl(nestedUri.ToString());
            }
        }

        return uri.ToString();
    }

    public static bool IsInvalidProductName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return true;
        }

        var trimmed = name.Trim();
        if (trimmed.All(char.IsDigit))
        {
            return true;
        }

        if (!trimmed.Contains(' ', StringComparison.Ordinal)
            && trimmed.Length <= 24
            && trimmed.All(char.IsLetterOrDigit))
        {
            return true;
        }

        var normalized = trimmed.ToLowerInvariant();
        if (normalized.Equals("produto", StringComparison.Ordinal))
        {
            return true;
        }

        foreach (var invalid in InvalidTitles)
        {
            if (invalid.Equals("produto", StringComparison.Ordinal))
            {
                continue;
            }

            if (normalized.Equals(invalid, StringComparison.Ordinal)
                || normalized.Contains(invalid, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<ProductMetadataDto?> TryExtractFromShopeeItemApiAsync(
        string shopId,
        string itemId,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(IntegrationHttpClientNames.ProductMetadata);
            var apiUrl = $"{ShopeeItemApiUrl}?itemid={Uri.EscapeDataString(itemId)}&shopid={Uri.EscapeDataString(shopId)}";
            using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            request.Headers.UserAgent.Clear();
            request.Headers.Remove("User-Agent");
            request.Headers.TryAddWithoutValidation("User-Agent", ShopeeApiUserAgent);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
            request.Headers.TryAddWithoutValidation("Accept-Language", "pt-BR,pt;q=0.9,en-US;q=0.8");
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "API de item Shopee recusada. Status={Status} ShopId={ShopId} ItemId={ItemId}",
                    (int)response.StatusCode,
                    shopId,
                    itemId);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return TryParseShopeeItemApiJson(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Falha ao consultar API de item Shopee. ShopId={ShopId} ItemId={ItemId}",
                shopId,
                itemId);
            return null;
        }
    }

    public static ProductMetadataDto? TryParseShopeeItemApiJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("data", out var data)
                || data.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return null;
            }

            var name = ReadJsonString(data, "name");
            var price = ReadShopeePrice(data, "price") ?? ReadShopeePrice(data, "price_min");
            var imageHash = ReadJsonString(data, "image");
            string? imageUrl = null;
            if (!string.IsNullOrWhiteSpace(imageHash))
            {
                imageUrl = imageHash.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? imageHash
                    : ShopeeImageCdn + imageHash.Trim().TrimStart('/');
            }

            if (IsInvalidProductName(name) && price is null && string.IsNullOrWhiteSpace(imageUrl))
            {
                return null;
            }

            return new ProductMetadataDto
            {
                ProductName = name,
                ProductPrice = price,
                ProductImageUrl = imageUrl
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static ProductMetadataDto SanitizeMetadata(ProductMetadataDto parsed, string resolvedUrl)
    {
        if (IsInvalidProductName(parsed.ProductName))
        {
            ApplyExpandedUrlName(parsed, resolvedUrl);
        }

        if (IsInvalidProductName(parsed.ProductName))
        {
            var unwrapped = UnwrapNestedDestinationUrl(resolvedUrl);
            ApplyExpandedUrlName(parsed, unwrapped);
        }

        if (IsInvalidProductName(parsed.ProductName))
        {
            parsed.ProductName = null;
        }

        return parsed;
    }

    private static void ApplyExpandedUrlName(ProductMetadataDto parsed, string resolvedUrl)
    {
        var slugName = ProductMetadataHtmlParser.TryExtractMarketplaceProductNameFromUrl(resolvedUrl);
        if (!IsInvalidProductName(slugName))
        {
            parsed.ProductName = slugName;
            return;
        }

        var fallback = ProductMetadataHtmlParser.BuildSlugFallback(resolvedUrl);
        parsed.ProductName = IsInvalidProductName(fallback) ? null : fallback;
    }

    private static void ApplyAntiBotBrowserHeaders(HttpRequestMessage request)
    {
        request.Headers.TryAddWithoutValidation(
            "User-Agent",
            "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1");
        request.Headers.TryAddWithoutValidation(
            "Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        request.Headers.TryAddWithoutValidation("Accept-Language", "pt-BR,pt;q=0.9,en-US;q=0.8");
    }

    private static decimal? ReadShopeePrice(JsonElement data, string propertyName)
    {
        if (!TryGetPropertyIgnoreCase(data, propertyName, out var priceElement))
        {
            return null;
        }

        if (priceElement.ValueKind == JsonValueKind.Number
            && priceElement.TryGetDecimal(out var numeric))
        {
            return ConvertShopeePrice(numeric);
        }

        if (priceElement.ValueKind == JsonValueKind.String
            && decimal.TryParse(
                priceElement.GetString(),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var parsed))
        {
            return ConvertShopeePrice(parsed);
        }

        return null;
    }

    private static string? ReadJsonString(JsonElement data, string propertyName)
    {
        if (!TryGetPropertyIgnoreCase(data, propertyName, out var element)
            || element.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var value = element.GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            value = default;
            return false;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool IsPositiveId(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.All(char.IsDigit) && value.TrimStart('0').Length > 0;

    private static string HttpUtilityUrlDecode(string value)
    {
        var current = value ?? string.Empty;
        for (var attempt = 0; attempt < 4; attempt++)
        {
            string decoded;
            try
            {
                decoded = Uri.UnescapeDataString(current.Replace("+", "%20"));
            }
            catch (UriFormatException)
            {
                decoded = current;
            }

            decoded = WebUtility.UrlDecode(decoded) ?? decoded;
            if (string.Equals(decoded, current, StringComparison.Ordinal))
            {
                break;
            }

            current = decoded;
        }

        return current;
    }
}
