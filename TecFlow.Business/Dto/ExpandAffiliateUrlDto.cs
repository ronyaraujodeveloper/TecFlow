using System.Text.Json.Serialization;

namespace TecFlow.Business.Dto;

public class ExpandAffiliateUrlRequestDto
{
    public string Url { get; set; } = string.Empty;

    public string Platform { get; set; } = string.Empty;
}

public class ExpandAffiliateUrlResponseDto
{
    [JsonPropertyName("status")]
    public bool Status { get; set; }

    [JsonPropertyName("descricao")]
    public string Descricao { get; set; } = string.Empty;

    [JsonPropertyName("expandedUrl")]
    public string ExpandedUrl { get; set; } = string.Empty;

    [JsonPropertyName("extractedId")]
    public string ExtractedId { get; set; } = string.Empty;
}
