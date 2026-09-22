namespace TecFlow.Business.Dto;

/// <summary>Envelope de resposta para geração de link de afiliado.</summary>
public class GerarLinkAfiliadoResponseDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string ShortenedUrl { get; set; } = string.Empty;

    /// <summary>Alias homologação (`POST /api/links/convert`).</summary>
    public string ConvertedUrl { get; set; } = string.Empty;

    public bool Status { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public string PlatformDetected { get; set; } = string.Empty;

    public Guid AffiliateLinkId { get; set; }

    public string ResolvedShortUrl =>
        FirstNonEmpty(ShortenedUrl, ConvertedUrl);

    public bool HasConvertedLink =>
        !string.IsNullOrWhiteSpace(ResolvedShortUrl);

    public void NormalizeHttp200()
    {
        if (!Success && Status)
        {
            Success = true;
        }

        if (string.IsNullOrWhiteSpace(ShortenedUrl) && !string.IsNullOrWhiteSpace(ConvertedUrl))
        {
            ShortenedUrl = ConvertedUrl;
        }

        if (string.IsNullOrWhiteSpace(ConvertedUrl) && !string.IsNullOrWhiteSpace(ShortenedUrl))
        {
            ConvertedUrl = ShortenedUrl;
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
