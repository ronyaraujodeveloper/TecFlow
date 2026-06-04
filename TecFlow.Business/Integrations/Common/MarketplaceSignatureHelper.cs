using System.Security.Cryptography;
using System.Text;

namespace TecFlow.Business.Integrations.Common;

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

    public static string GenerateTikTokShopSign(
        string appKey,
        string appSecret,
        string apiPath,
        long timestamp,
        string? requestBody = null)
    {
        var path = NormalizeApiPath(apiPath);
        var baseString = $"{appKey}{path}{timestamp}{requestBody ?? string.Empty}";
        return ComputeHmacSha256Hex(appSecret, baseString);
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
