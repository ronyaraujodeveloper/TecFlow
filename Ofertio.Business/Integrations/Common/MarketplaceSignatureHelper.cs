using System.Security.Cryptography;
using System.Text;

namespace Ofertio.Business.Integrations.Common;

/// <summary>Implementação estática de assinatura HMAC-SHA256 para marketplaces.</summary>
public static class MarketplaceSignatureHelper
{
    public static string GenerateShopeeSign(
        string partnerId,
        string partnerKey,
        string apiPath,
        long timestamp,
        string? accessToken = null,
        string? shopId = null)
    {
        var path = NormalizeApiPath(apiPath);
        var baseString = $"{partnerId}{path}{timestamp}";

        if (!string.IsNullOrEmpty(accessToken))
        {
            baseString += accessToken;
        }

        if (!string.IsNullOrEmpty(shopId))
        {
            baseString += shopId;
        }

        return ComputeHmacSha256Hex(partnerKey, baseString);
    }

    /// <summary>
    /// Assinatura TikTok Shop 202309 com os parâmetros comuns (<c>app_key</c>, <c>timestamp</c>) e os extras da query
    /// (ex.: <c>shop_cipher</c>, <c>page_size</c>).
    /// </summary>
    public static string GenerateTikTokShopSign(
        string appKey,
        string appSecret,
        string apiPath,
        long timestamp,
        string? requestBody = null,
        IReadOnlyDictionary<string, string?>? extraQuery = null)
    {
        var query = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["app_key"] = appKey,
            ["timestamp"] = timestamp.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
        if (extraQuery is not null)
        {
            foreach (var (key, value) in extraQuery)
            {
                query[key] = value;
            }
        }

        return ComputeTikTokShopSign(appSecret, apiPath, query, requestBody);
    }

    /// <summary>
    /// Especificação TikTok Shop 202309: HMAC-SHA256 (chave = app_secret) de
    /// <c>app_secret + path + chaves da query em ordem alfabética (chave+valor, sem sign e access_token) + body + app_secret</c>.
    /// </summary>
    public static string ComputeTikTokShopSign(
        string appSecret,
        string apiPath,
        IEnumerable<KeyValuePair<string, string?>> query,
        string? requestBody = null)
    {
        var builder = new StringBuilder();
        builder.Append(appSecret);
        builder.Append(NormalizeApiPath(apiPath));
        foreach (var (key, value) in query
                     .Where(pair => !string.IsNullOrEmpty(pair.Key)
                                    && pair.Value is not null
                                    && pair.Key is not "sign" and not "access_token")
                     .OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            builder.Append(key).Append(value);
        }

        builder.Append(requestBody ?? string.Empty);
        builder.Append(appSecret);
        return ComputeHmacSha256Hex(appSecret, builder.ToString());
    }

    private static string NormalizeApiPath(string apiPath)
    {
        var path = apiPath.Trim();
        if (!path.StartsWith('/'))
        {
            path = "/" + path;
        }

        return path;
    }

    public static string GenerateShopeeWebhookSign(string partnerKey, string callbackUrl, string rawBody) =>
        ComputeHmacSha256Hex(partnerKey, $"{callbackUrl}|{rawBody}");

    public static string GenerateTikTokShopWebhookSign(string secret, string rawBody) =>
        ComputeHmacSha256Hex(secret, rawBody);

    public static bool CompareHexSignatures(string computedHex, string headerSignature)
    {
        if (string.IsNullOrWhiteSpace(headerSignature))
        {
            return false;
        }

        var normalized = headerSignature.Trim()
            .Replace("SHA256 ", string.Empty, StringComparison.OrdinalIgnoreCase)
            .ToLowerInvariant();

        try
        {
            var computedBytes = Convert.FromHexString(computedHex.ToLowerInvariant());
            var expectedBytes = Convert.FromHexString(normalized);
            return computedBytes.Length == expectedBytes.Length &&
                   CryptographicOperations.FixedTimeEquals(computedBytes, expectedBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static bool VerifyHmacSha256Hex(string secret, string payload, string expectedHexSignature)
    {
        if (string.IsNullOrWhiteSpace(expectedHexSignature))
        {
            return false;
        }

        var computed = ComputeHmacSha256Hex(secret, payload);
        var normalizedExpected = expectedHexSignature.Trim()
            .Replace("SHA256 ", string.Empty, StringComparison.OrdinalIgnoreCase)
            .ToLowerInvariant();

        try
        {
            var computedBytes = Convert.FromHexString(computed);
            var expectedBytes = Convert.FromHexString(normalizedExpected);
            return computedBytes.Length == expectedBytes.Length &&
                   CryptographicOperations.FixedTimeEquals(computedBytes, expectedBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string ComputeHmacSha256Hex(string secret, string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
