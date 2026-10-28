using System.Globalization;
using System.Text.Json;
using Ofertio.Business.Dto;

namespace Ofertio.Business.Integrations.TikTokShop;

/// <summary>
/// Cache global das buscas do TikTok Shop com o token Master: a mesma consulta (termo normalizado + filtros) é servida
/// da memória dentro do tempo configurado no Painel Master, sem consumir a cota da API.
/// </summary>
public static class TikTokShopSearchCacheRules
{
    public const string SettingKey = TikTokShopIntegrationOptions.SectionName + ":SearchCacheMinutes";

    public const int DefaultMinutes = 15;

    public const int MaxMinutes = 1440;

    private const string KeyPrefix = "tts-search:";

    private static readonly JsonSerializerOptions CloneOptions = new();

    public static int ClampMinutes(int minutes) => Math.Clamp(minutes, 0, MaxMinutes);

    /// <summary>Valor configurado limitado a 0..1440; vazio ou não numérico usa o padrão.</summary>
    public static int ResolveMinutes(string? raw) =>
        int.TryParse(raw?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? ClampMinutes(parsed)
            : DefaultMinutes;

    public static string NormalizeQuery(string? query) =>
        string.Join(' ', (query ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    public static string BuildKey(TikTokShopProductSearchRequest request) =>
        string.Join('|',
            KeyPrefix + NormalizeQuery(request.Query),
            request.PageToken?.Trim() ?? string.Empty,
            FormatPrice(request.MinPrice),
            FormatPrice(request.MaxPrice),
            request.MinCommissionRate?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            request.PageSize.ToString(CultureInfo.InvariantCulture));

    public static bool IsCacheable(OfficialCatalogChannelResult result) =>
        !result.MissingCredentials && string.IsNullOrWhiteSpace(result.ErrorMessage);

    /// <summary>Cópia profunda, para que o cache nunca seja alterado por quem consome o resultado.</summary>
    public static OfficialCatalogChannelResult Clone(OfficialCatalogChannelResult result) =>
        JsonSerializer.Deserialize<OfficialCatalogChannelResult>(
            JsonSerializer.Serialize(result, CloneOptions),
            CloneOptions) ?? OfficialCatalogChannelResult.Empty;

    private static string FormatPrice(decimal? value) =>
        value?.ToString("0.##", CultureInfo.InvariantCulture) ?? string.Empty;
}
