using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TecFlow.Business.Dto;
using TecFlow.Business.Integrations.Shopee;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Radar;
using TecFlow.Core.Enums;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Radar;

public sealed class ShopeeAffiliateOfferService : IShopeeAffiliateOfferService
{
    private readonly HttpClient _http;
    private readonly ShopeeIntegrationOptions _options;
    private readonly AppDbContext _context;
    private readonly ILogger<ShopeeAffiliateOfferService> _logger;

    public ShopeeAffiliateOfferService(
        HttpClient http,
        IOptions<ShopeeIntegrationOptions> options,
        AppDbContext context,
        ILogger<ShopeeAffiliateOfferService> logger)
    {
        _http = http;
        _options = options.Value;
        _context = context;
        _logger = logger;
    }

    public async Task<OfficialOfferSnapshotDto?> GetProductOfferAsync(
        int userId,
        string shopId,
        string itemId,
        CancellationToken cancellationToken = default)
    {
        var appId = _options.ResolveAffiliateAppId();
        var secret = _options.ResolveAffiliateSecret();
        if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(secret) || _options.IsSandboxMode)
        {
            var account = await _context.MarketplaceAccounts.AsNoTracking()
                .Where(item => item.IsActive && item.MarketplaceType == MarketplaceType.Shopee
                    && item.UserId == userId.ToString() && item.AppKey != null && item.AppSecret != null)
                .FirstOrDefaultAsync(cancellationToken);
            appId = account?.AppKey ?? appId;
            secret = account?.AppSecret ?? secret;
        }

        if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(secret))
        {
            return null;
        }

        try
        {
            var query =
                "{ productOfferV2(shopId: " + shopId + ", itemId: " + itemId
                + ") { nodes { productName priceMin commissionRate productStatus shopName } } }";
            var jsonBody = JsonSerializer.Serialize(new { query });
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var sign = Sign(appId, secret, timestamp, jsonBody);
            using var request = new HttpRequestMessage(HttpMethod.Post, "graphql");
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            request.Headers.TryAddWithoutValidation("Authorization", sign);
            request.Headers.TryAddWithoutValidation("timestamp", timestamp.ToString());
            using var response = await _http.SendAsync(request, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Shopee productOfferV2 falhou. Status={Status}", (int)response.StatusCode);
                return null;
            }

            return Parse(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha no productOfferV2. Shop={Shop} Item={Item}", shopId, itemId);
            return null;
        }
    }

    public static OfficialOfferSnapshotDto? Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("data", out var data)
            || !data.TryGetProperty("productOfferV2", out var offer)
            || !offer.TryGetProperty("nodes", out var nodes)
            || nodes.ValueKind != JsonValueKind.Array
            || nodes.GetArrayLength() == 0)
        {
            return null;
        }

        var node = nodes[0];
        var status = node.TryGetProperty("productStatus", out var st) ? st.GetString() : "active";
        var available = !string.Equals(status, "INACTIVE", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(status, "DELETED", StringComparison.OrdinalIgnoreCase);
        decimal? price = null;
        if (node.TryGetProperty("priceMin", out var priceEl))
        {
            if (priceEl.ValueKind == JsonValueKind.Number && priceEl.TryGetDecimal(out var numeric))
            {
                price = numeric;
            }
            else if (priceEl.ValueKind == JsonValueKind.String
                && decimal.TryParse(priceEl.GetString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            {
                price = parsed;
            }
        }

        return new OfficialOfferSnapshotDto
        {
            IsAvailable = available,
            Status = LiveSearchRules.MapListingStatus(status, available),
            Price = price,
            ProductName = node.TryGetProperty("productName", out var name) ? name.GetString() : null,
            CouponCode = node.TryGetProperty("commissionRate", out _) ? null : null,
            Source = "Api",
            Platform = MarketplaceType.Shopee
        };
    }

    private static string Sign(string appId, string secret, long timestamp, string jsonBody)
    {
        var payload = $"{appId}{timestamp}{jsonBody}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }
}
