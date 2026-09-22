using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TecFlow.Business.Integrations.Shopee;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Database.Entity;

namespace TecFlow.Infrastructure.Services.LinkStrategies;

/// <summary>Integração REST com generateCustomLink da Shopee Affiliate Open API.</summary>
public sealed class ShopeeAffiliateLinkClient : IShopeeAffiliateLinkClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ShopeeIntegrationOptions _options;
    private readonly ILogger<ShopeeAffiliateLinkClient> _logger;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IAffiliateLinkGenerationContext? _generationContext;

    public ShopeeAffiliateLinkClient(
        HttpClient httpClient,
        IOptions<ShopeeIntegrationOptions> options,
        ILogger<ShopeeAffiliateLinkClient> logger,
        IHostEnvironment hostEnvironment,
        IAffiliateLinkGenerationContext? generationContext = null)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _hostEnvironment = hostEnvironment;
        _generationContext = generationContext;
    }

    public async Task<string> GenerateCustomLinkAsync(
        IntegracaoLoja store,
        string expandedProductUrl,
        string affiliateId,
        string? customNickname,
        CancellationToken cancellationToken = default)
    {
        var appId = _options.ResolveAffiliateAppId();
        var secret = _options.ResolveAffiliateSecret();

        if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(secret) || _options.IsSandboxMode)
        {
            _logger.LogInformation(
                "Shopee sandbox: credenciais de afiliado vazias. Gerando URL de homologação com {TrackingCode}.",
                _options.SandboxTrackingCode);

            return ApplyCommissionTracking(expandedProductUrl, store, includeHomologAffiliate: true);
        }

        try
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var subIds = BuildSubIds(
                affiliateId,
                customNickname,
                store.ShopId,
                ShopeeCommissionUrlBuilder.BuildSubId(store.UserId, store.TenantId));
            var payload = new
            {
                productUrl = expandedProductUrl,
                subIds,
                shopId = store.ShopId
            };

            var jsonBody = JsonSerializer.Serialize(payload, JsonOptions);
            var sign = ComputeAffiliateSign(appId, secret, timestamp, jsonBody);
            var requestUri =
                $"{_options.GenerateCustomLinkPath.TrimStart('/')}?appId={Uri.EscapeDataString(appId)}&timestamp={timestamp}&sign={sign}";

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {store.AccessToken}");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Shopee generateCustomLink falhou. Status={StatusCode} StoreId={StoreId} ShopId={ShopId} ProductUrl={ProductUrl} Body={Body}",
                    (int)response.StatusCode,
                    store.Id,
                    store.ShopId,
                    expandedProductUrl,
                    content);

                return ApplyCommissionTracking(expandedProductUrl, store, includeHomologAffiliate: true);
            }

            var link = TryExtractAffiliateUrl(content);
            CaptureOfficialShortFromPayload(content, link);
            if (string.IsNullOrWhiteSpace(link))
            {
                _logger.LogError(
                    "Shopee não retornou customLink. StoreId={StoreId} ShopId={ShopId} ProductUrl={ProductUrl} Homolog={Homolog}",
                    store.Id,
                    store.ShopId,
                    expandedProductUrl,
                    IsHomologEnvironment());
                return ApplyCommissionTracking(expandedProductUrl, store, includeHomologAffiliate: true);
            }

            var source = ShopeeOfficialShortUrl.IsOfficialShortener(link) ? expandedProductUrl : link;
            return ApplyCommissionTracking(source, store, includeHomologAffiliate: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falha na API de afiliados Shopee. StoreId={StoreId} ShopId={ShopId} ProductUrl={ProductUrl} ExceptionType={ExceptionType} Causa={Causa}",
                store.Id,
                store.ShopId,
                expandedProductUrl,
                ex.GetType().FullName,
                ex.ToString());
            return ApplyCommissionTracking(expandedProductUrl, store, includeHomologAffiliate: true);
        }
    }

    private bool IsHomologEnvironment() =>
        _hostEnvironment.IsDevelopment()
        || _hostEnvironment.IsEnvironment("Homologacao");

    private string ApplyCommissionTracking(string url, IntegracaoLoja store, bool includeHomologAffiliate = false)
    {
        var parsed = ShopeeProductUrlParser.TryParse(url, out var ids);
        if (includeHomologAffiliate && !parsed)
        {
            return ShopeeCommissionUrlBuilder.BuildHomologConvertedLink(
                ShopeeProductUrlParser.TryExtractShortHash(url));
        }

        var productUrl = parsed
            ? ShopeeCommissionUrlBuilder.ToUniversalWebUrl(ids)
            : url;
        var source = includeHomologAffiliate ? productUrl : url;
        var universal = parsed
            ? ShopeeCommissionUrlBuilder.ToUniversalWebUrl(ids)
            : url;

        return ShopeeCommissionUrlBuilder.Merge(
            source,
            _options.SandboxTrackingCode,
            ShopeeCommissionUrlBuilder.BuildSubId(store.UserId, store.TenantId),
            universal,
            affiliateId: includeHomologAffiliate ? ShopeeCommissionUrlBuilder.HomologAffiliateId : null);
    }

    private static IReadOnlyList<string> BuildSubIds(
        string affiliateId,
        string? customNickname,
        string shopId,
        string attributionSubId)
    {
        var subIds = new List<string> { attributionSubId, affiliateId, shopId };
        if (!string.IsNullOrWhiteSpace(customNickname))
        {
            subIds.Add(customNickname.Trim());
        }

        return subIds;
    }

    private void CaptureOfficialShortFromPayload(string content, string? customLink)
    {
        if (_generationContext is null)
        {
            return;
        }

        var official = TryExtractOfficialShortUrl(content);
        if (string.IsNullOrWhiteSpace(official) && ShopeeOfficialShortUrl.IsOfficialShortener(customLink))
        {
            official = customLink;
        }

        if (!string.IsNullOrWhiteSpace(official) && ShopeeOfficialShortUrl.IsOfficialShortener(official))
        {
            _generationContext.OfficialShortenedShopeeUrl = ShopeeOfficialShortUrl.Sanitize(official);
        }
    }

    private static string? TryExtractOfficialShortUrl(string content)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            if (root.TryGetProperty("data", out var data))
            {
                foreach (var name in new[] { "shortLink", "short_link", "shortUrl", "short_url" })
                {
                    if (data.TryGetProperty(name, out var value))
                    {
                        var text = value.GetString();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            return text;
                        }
                    }
                }
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private static string? TryExtractAffiliateUrl(string content)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.TryGetProperty("data", out var data))
            {
                if (data.TryGetProperty("customLink", out var customLink))
                {
                    return customLink.GetString();
                }

                if (data.TryGetProperty("affiliate_url", out var affiliateUrl))
                {
                    return affiliateUrl.GetString();
                }

                if (data.TryGetProperty("shortLink", out var shortLink))
                {
                    return shortLink.GetString();
                }
            }

            if (root.TryGetProperty("customLink", out var rootCustomLink))
            {
                return rootCustomLink.GetString();
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private static string ComputeAffiliateSign(string appId, string secret, long timestamp, string jsonBody)
    {
        var baseString = $"{appId}{timestamp}{jsonBody}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(baseString));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
