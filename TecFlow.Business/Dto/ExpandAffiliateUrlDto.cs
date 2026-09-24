namespace TecFlow.Business.Dto;

public class ExpandAffiliateUrlRequestDto
{
    public string Url { get; set; } = string.Empty;

    public string Platform { get; set; } = string.Empty;
}

public class ExpandAffiliateUrlResponseDto
{
    public bool Status { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public string ExpandedUrl { get; set; } = string.Empty;

    public string ExtractedId { get; set; } = string.Empty;
}
