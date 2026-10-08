using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TecFlow.Business.Dto;
using TecFlow.Business.Integrations.Amazon;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Radar;
using TecFlow.Core.Enums;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Radar;

public sealed class AmazonPaApiService : IAmazonPaApiService
{
    private readonly HttpClient _http;
    private readonly AmazonPaApiOptions _options;
    private readonly AppDbContext _context;
    private readonly ILogger<AmazonPaApiService> _logger;

    public AmazonPaApiService(
        HttpClient http,
        IOptions<AmazonPaApiOptions> options,
        AppDbContext context,
        ILogger<AmazonPaApiService> logger)
    {
        _http = http;
        _options = options.Value;
        _context = context;
        _logger = logger;
    }

    public async Task<OfficialOfferSnapshotDto?> GetItemAsync(
        int userId,
        string asin,
        CancellationToken cancellationToken = default)
    {
        var access = _options.AccessKey;
        var secret = _options.SecretKey;
        var tag = _options.PartnerTag;
        if (!_options.HasCredentials)
        {
            var account = await _context.MarketplaceAccounts.AsNoTracking()
                .Where(item => item.IsActive && item.MarketplaceType == MarketplaceType.Amazon
                    && item.UserId == userId.ToString())
                .FirstOrDefaultAsync(cancellationToken);
            access = First(account?.AppKey, access);
            secret = First(account?.AppSecret, secret);
            tag = First(account?.TrackingId, account?.AffiliateTrackingId, tag);
        }

        if (string.IsNullOrWhiteSpace(access) || string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(tag))
        {
            return null;
        }

        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                PartnerTag = tag,
                PartnerType = "Associates",
                Marketplace = _options.Marketplace,
                ItemIds = new[] { asin },
                Resources = new[] { "ItemInfo.Title", "Offers.Listings.Price" }
            });
            using var request = BuildSignedRequest(access, secret, payload, "/paapi5/getitems", "com.amazon.paapi5.v1.ProductAdvertisingAPIv1.GetItems");
            using var response = await _http.SendAsync(request, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Amazon PA-API GetItems falhou. Status={Status} Asin={Asin}", (int)response.StatusCode, asin);
                return null;
            }

            return Parse(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha na PA-API. Asin={Asin}", asin);
            return null;
        }
    }

    public async Task<IReadOnlyList<OfficialCatalogProductDto>> SearchProductsAsync(
        int userId,
        string query,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var access = _options.AccessKey;
        var secret = _options.SecretKey;
        var tag = _options.PartnerTag;
        if (!_options.HasCredentials)
        {
            var account = await _context.MarketplaceAccounts.AsNoTracking()
                .Where(item => item.IsActive && item.MarketplaceType == MarketplaceType.Amazon
                    && item.UserId == userId.ToString())
                .FirstOrDefaultAsync(cancellationToken);
            access = First(account?.AppKey, access);
            secret = First(account?.AppSecret, secret);
            tag = First(account?.TrackingId, account?.AffiliateTrackingId, tag);
        }

        if (string.IsNullOrWhiteSpace(access) || string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(tag))
        {
            return [];
        }

        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                PartnerTag = tag,
                PartnerType = "Associates",
                Marketplace = _options.Marketplace,
                Keywords = query.Trim(),
                SearchIndex = "All",
                ItemCount = OfficialCatalogSearchRules.ClampLimit(limit),
                Resources = new[]
                {
                    "ItemInfo.Title",
                    "Offers.Listings.Price",
                    "Images.Primary.Large",
                    "Images.Primary.Medium"
                }
            });
            using var request = BuildSignedRequest(
                access,
                secret,
                payload,
                "/paapi5/searchitems",
                "com.amazon.paapi5.v1.ProductAdvertisingAPIv1.SearchItems");
            using var response = await _http.SendAsync(request, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Amazon PA-API SearchItems falhou. Status={Status}", (int)response.StatusCode);
                return [];
            }

            return ParseSearch(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha no SearchItems. Query={Query}", query);
            return [];
        }
    }

    internal static OfficialOfferSnapshotDto? Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("ItemsResult", out var result)
            || !result.TryGetProperty("Items", out var items)
            || items.GetArrayLength() == 0)
        {
            return null;
        }

        var item = items[0];
        string? title = null;
        if (item.TryGetProperty("ItemInfo", out var info)
            && info.TryGetProperty("Title", out var titleEl)
            && titleEl.TryGetProperty("DisplayValue", out var display))
        {
            title = display.GetString();
        }

        decimal? price = null;
        var available = true;
        if (item.TryGetProperty("Offers", out var offers)
            && offers.TryGetProperty("Listings", out var listings)
            && listings.GetArrayLength() > 0)
        {
            var listing = listings[0];
            if (listing.TryGetProperty("Price", out var priceEl)
                && priceEl.TryGetProperty("Amount", out var amount)
                && amount.TryGetDecimal(out var value))
            {
                price = value;
            }
        }
        else
        {
            available = false;
        }

        return new OfficialOfferSnapshotDto
        {
            IsAvailable = available,
            Status = LiveSearchRules.MapListingStatus(available ? "active" : "out_of_stock", available),
            Price = price,
            ProductName = title,
            Source = "Api",
            Platform = MarketplaceType.Amazon
        };
    }

    public static IReadOnlyList<OfficialCatalogProductDto> ParseSearch(string json)
    {
        var list = new List<OfficialCatalogProductDto>();
        using var doc = JsonDocument.Parse(json);
        JsonElement items;
        if (doc.RootElement.TryGetProperty("SearchResult", out var search)
            && search.TryGetProperty("Items", out items)
            && items.ValueKind == JsonValueKind.Array)
        {
        }
        else if (doc.RootElement.TryGetProperty("ItemsResult", out var result)
            && result.TryGetProperty("Items", out items)
            && items.ValueKind == JsonValueKind.Array)
        {
        }
        else
        {
            return list;
        }

        foreach (var item in items.EnumerateArray())
        {
            var asin = item.TryGetProperty("ASIN", out var asinEl) ? asinEl.GetString() : null;
            var url = item.TryGetProperty("DetailPageURL", out var urlEl) ? urlEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(url) && !string.IsNullOrWhiteSpace(asin))
            {
                url = "https://www.amazon.com.br/dp/" + asin;
            }

            if (string.IsNullOrWhiteSpace(url))
            {
                continue;
            }

            string? title = null;
            if (item.TryGetProperty("ItemInfo", out var info)
                && info.TryGetProperty("Title", out var titleEl)
                && titleEl.TryGetProperty("DisplayValue", out var display))
            {
                title = display.GetString();
            }

            decimal? price = null;
            if (item.TryGetProperty("Offers", out var offers)
                && offers.TryGetProperty("Listings", out var listings)
                && listings.GetArrayLength() > 0
                && listings[0].TryGetProperty("Price", out var priceEl)
                && priceEl.TryGetProperty("Amount", out var amount)
                && amount.TryGetDecimal(out var value))
            {
                price = value;
            }

            string? image = null;
            if (item.TryGetProperty("Images", out var images)
                && images.TryGetProperty("Primary", out var primary))
            {
                if (primary.TryGetProperty("Large", out var large) && large.TryGetProperty("URL", out var largeUrl))
                {
                    image = largeUrl.GetString();
                }
                else if (primary.TryGetProperty("Medium", out var medium) && medium.TryGetProperty("URL", out var mediumUrl))
                {
                    image = mediumUrl.GetString();
                }
            }

            list.Add(new OfficialCatalogProductDto
            {
                Platform = nameof(MarketplaceType.Amazon),
                PlatformType = MarketplaceType.Amazon,
                ProductId = asin,
                ProductName = title,
                Price = price,
                ImageUrl = image,
                SourceUrl = url,
                Source = "Api"
            });
        }

        return list;
    }

    private HttpRequestMessage BuildSignedRequest(
        string accessKey,
        string secretKey,
        string payload,
        string path,
        string target)
    {
        var now = DateTime.UtcNow;
        var amzDate = now.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        var dateStamp = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var host = string.IsNullOrWhiteSpace(_options.Host) ? "webservices.amazon.com.br" : _options.Host.Trim();
        var region = string.IsNullOrWhiteSpace(_options.Region) ? "us-east-1" : _options.Region.Trim();
        const string service = "ProductAdvertisingAPI";
        var payloadHash = Sha256Hex(payload);
        var canonicalHeaders =
            "content-encoding:amz-1.0\n"
            + "content-type:application/json; charset=utf-8\n"
            + "host:" + host + "\n"
            + "x-amz-date:" + amzDate + "\n"
            + "x-amz-target:" + target + "\n";
        var signedHeaders = "content-encoding;content-type;host;x-amz-date;x-amz-target";
        var canonicalRequest = "POST\n" + path + "\n\n" + canonicalHeaders + "\n" + signedHeaders + "\n" + payloadHash;
        var credentialScope = dateStamp + "/" + region + "/" + service + "/aws4_request";
        var stringToSign = "AWS4-HMAC-SHA256\n" + amzDate + "\n" + credentialScope + "\n" + Sha256Hex(canonicalRequest);
        var signingKey = GetSignatureKey(secretKey, dateStamp, region, service);
        var signature = ToHex(Hmac(signingKey, stringToSign));
        var authorization =
            "AWS4-HMAC-SHA256 Credential=" + accessKey + "/" + credentialScope
            + ", SignedHeaders=" + signedHeaders + ", Signature=" + signature;

        var request = new HttpRequestMessage(HttpMethod.Post, "https://" + host + path);
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json")
        {
            CharSet = "utf-8"
        };
        request.Headers.TryAddWithoutValidation("Authorization", authorization);
        request.Headers.TryAddWithoutValidation("Host", host);
        request.Headers.TryAddWithoutValidation("X-Amz-Date", amzDate);
        request.Headers.TryAddWithoutValidation("X-Amz-Target", target);
        request.Headers.TryAddWithoutValidation("Content-Encoding", "amz-1.0");
        return request;
    }

    private static byte[] GetSignatureKey(string key, string dateStamp, string region, string service)
    {
        var kDate = Hmac(Encoding.UTF8.GetBytes("AWS4" + key), dateStamp);
        var kRegion = Hmac(kDate, region);
        var kService = Hmac(kRegion, service);
        return Hmac(kService, "aws4_request");
    }

    private static byte[] Hmac(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }

    private static string Sha256Hex(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return ToHex(hash);
    }

    private static string ToHex(byte[] bytes) => Convert.ToHexString(bytes).ToLowerInvariant();

    private static string? First(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
