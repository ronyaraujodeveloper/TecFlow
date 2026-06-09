namespace TecFlow.Business.Dto;

/// <summary>Envelope de resposta para geração de link de afiliado.</summary>
public class GerarLinkAfiliadoResponseDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string ShortenedUrl { get; set; } = string.Empty;

    public string PlatformDetected { get; set; } = string.Empty;

    public Guid AffiliateLinkId { get; set; }
}
