namespace TecFlow.Business.Dto;

/// <summary>Envelope de resposta para geração de link de afiliado.</summary>
public class GerarLinkAfiliadoResponseDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    /// <summary>URL enviada pelo usuário.</summary>
    public string OriginalUrl { get; set; } = string.Empty;

    /// <summary>URL oficial de afiliado da Shopee (tag de rastreio).</summary>
    public string AffiliateUrl { get; set; } = string.Empty;

    /// <summary>URL interna TecFlow de telemetria (http://localhost:5001/r/code).</summary>
    public string ShortenedUrl { get; set; } = string.Empty;

    /// <summary>URL encurtada oficial da Shopee (https://br.shp.ee/...).</summary>
    public string ShortenedShopeeUrl { get; set; } = string.Empty;

    /// <summary>Alias de <see cref="AffiliateUrl"/> para o contrato `POST /api/links/convert`.</summary>
    public string ConvertedUrl { get; set; } = string.Empty;

    public bool Status { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public string PlatformDetected { get; set; } = string.Empty;

    public Guid AffiliateLinkId { get; set; }

    public string ResolvedShortUrl =>
        FirstNonEmpty(ShortenedUrl, LooksLikeTecFlowShort(ConvertedUrl) ? ConvertedUrl : null);

    public bool HasConvertedLink =>
        !string.IsNullOrWhiteSpace(AffiliateUrl)
        || !string.IsNullOrWhiteSpace(ResolvedShortUrl)
        || !string.IsNullOrWhiteSpace(ShortenedShopeeUrl);

    public void NormalizeHttp200()
    {
        if (!Success && Status)
        {
            Success = true;
        }

        if (string.IsNullOrWhiteSpace(AffiliateUrl) && !string.IsNullOrWhiteSpace(ConvertedUrl) && !LooksLikeTecFlowShort(ConvertedUrl))
        {
            AffiliateUrl = ConvertedUrl.Trim();
        }

        if (string.IsNullOrWhiteSpace(ConvertedUrl) && !string.IsNullOrWhiteSpace(AffiliateUrl))
        {
            ConvertedUrl = AffiliateUrl;
        }

        if (string.IsNullOrWhiteSpace(ShortenedUrl) && LooksLikeTecFlowShort(ConvertedUrl))
        {
            ShortenedUrl = ConvertedUrl.Trim();
        }

        if (string.IsNullOrWhiteSpace(ShortenedShopeeUrl) && LooksLikeShopeeOfficialShort(ConvertedUrl))
        {
            ShortenedShopeeUrl = ConvertedUrl.Trim();
        }

        if (string.IsNullOrWhiteSpace(ShortenedShopeeUrl) && LooksLikeShopeeOfficialShort(AffiliateUrl))
        {
            ShortenedShopeeUrl = AffiliateUrl.Trim();
        }

        if (string.IsNullOrWhiteSpace(Message) && !string.IsNullOrWhiteSpace(Descricao))
        {
            Message = Descricao;
        }

        if (HasConvertedLink)
        {
            Success = true;
            Status = true;
        }
    }

    private static bool LooksLikeTecFlowShort(string? url) =>
        !string.IsNullOrWhiteSpace(url)
        && url.Contains("/r/", StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikeShopeeOfficialShort(string? url) =>
        !string.IsNullOrWhiteSpace(url)
        && (url.Contains("br.shp.ee", StringComparison.OrdinalIgnoreCase)
            || url.Contains("shp.ee/", StringComparison.OrdinalIgnoreCase)
            || url.Contains("s.shopee.com", StringComparison.OrdinalIgnoreCase));

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }
}
