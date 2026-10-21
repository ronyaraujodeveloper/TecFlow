using System.Globalization;
using System.Text.Json;
using Ofertio.Business.Dto;

namespace Ofertio.Business.Integrations.MercadoLivre;

/// <summary>
/// Cache global das buscas do Mercado Livre: a mesma consulta (termo normalizado + filtros) é servida da memória
/// dentro do tempo configurado no Painel Master, sem consumir a cota da API. O TrackingId fica fora da chave:
/// o resultado é guardado sem afiliado e o <c>matt_tool</c> de cada cliente é aplicado numa cópia.
/// </summary>
public static class MercadoLivreSearchCacheRules
{
    public const string SettingKey = MercadoLivreIntegrationOptions.SectionName + ":SearchCacheMinutes";

    public const int DefaultMinutes = 15;

    public const int MaxMinutes = 1440;

    private const string KeyPrefix = "ml-search:";

    private static readonly JsonSerializerOptions CloneOptions = new();

    public static int ClampMinutes(int minutes) => Math.Clamp(minutes, 0, MaxMinutes);

    /// <summary>Valor configurado limitado a 0..1440; vazio ou não numérico usa o padrão.</summary>
    public static int ResolveMinutes(string? raw) =>
        int.TryParse(raw?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? ClampMinutes(parsed)
            : DefaultMinutes;

    public static bool TryParseMinutes(string? raw, out int minutes)
    {
        if (int.TryParse(raw?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            && parsed is >= 0 and <= MaxMinutes)
        {
            minutes = parsed;
            return true;
        }

        minutes = 0;
        return false;
    }

    public static string NormalizeQuery(string? query) =>
        string.Join(' ', (query ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    public static string BuildKey(string query, MercadoLivreCatalogSearchRequest request) =>
        string.Join('|',
            KeyPrefix + NormalizeQuery(query),
            Math.Max(0, request.Offset).ToString(CultureInfo.InvariantCulture),
            request.TargetOffers.ToString(CultureInfo.InvariantCulture),
            (request.DomainId?.Trim() ?? string.Empty).ToUpperInvariant(),
            FormatPrice(request.MinPrice),
            FormatPrice(request.MaxPrice),
            Flag(request.FreeShippingOnly),
            Flag(request.MainProductsOnly),
            Flag(request.AllowShorterQuery),
            Flag(request.IncludeCategories),
            Flag(request.IncludeMetrics));

    /// <summary>Só resultados bem-sucedidos (sem erro nem falta de credencial) entram no cache.</summary>
    public static bool IsCacheable(OfficialCatalogChannelResult result) =>
        !result.MissingCredentials && string.IsNullOrWhiteSpace(result.ErrorMessage);

    /// <summary>Cópia profunda, para que o cache nunca seja alterado por quem consome o resultado.</summary>
    public static OfficialCatalogChannelResult Clone(OfficialCatalogChannelResult result) =>
        JsonSerializer.Deserialize<OfficialCatalogChannelResult>(
            JsonSerializer.Serialize(result, CloneOptions),
            CloneOptions) ?? OfficialCatalogChannelResult.Empty;

    /// <summary>Cópia do resultado com o <c>matt_tool</c> do cliente aplicado nos links.</summary>
    public static OfficialCatalogChannelResult ForAffiliate(OfficialCatalogChannelResult result, string? affiliateId) =>
        ApplyAffiliate(Clone(result), affiliateId);

    /// <summary>Aplica o <c>matt_tool</c> do cliente nos links do próprio resultado (que não pode estar no cache).</summary>
    public static OfficialCatalogChannelResult ApplyAffiliate(OfficialCatalogChannelResult result, string? affiliateId)
    {
        foreach (var item in result.Items)
        {
            item.SourceUrl = MercadoLivreCommissionUrlBuilder.InjectMattTool(item.SourceUrl, affiliateId);
        }

        return result;
    }

    private static string FormatPrice(decimal? value) =>
        value?.ToString("0.##", CultureInfo.InvariantCulture) ?? string.Empty;

    private static string Flag(bool value) => value ? "1" : "0";
}
