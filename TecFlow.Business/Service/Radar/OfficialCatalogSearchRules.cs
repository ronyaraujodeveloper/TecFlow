using TecFlow.Business.Dto;

namespace TecFlow.Business.Service.Radar;

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

    public static OfficialCatalogChannelStatusDto BuildChannelStatus(
        string key,
        string label,
        bool included,
        int productCount,
        bool missingCredentials,
        string missingMessage)
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

        if (productCount > 0)
        {
            return new OfficialCatalogChannelStatusDto
            {
                Key = key,
                Label = label,
                Included = true,
                ProductCount = productCount,
                State = "ok",
                Message = $"{productCount} produtos encontrados"
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
