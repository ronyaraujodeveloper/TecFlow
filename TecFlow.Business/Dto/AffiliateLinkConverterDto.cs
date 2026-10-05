using TecFlow.Core.Enums;

namespace TecFlow.Business.Dto;

public class AffiliateLinkConverterDto
{
    public string OriginalUrl { get; set; } = string.Empty;

    public string? SourceGroup { get; set; }
}

public class AffiliateLinkConverterResultDto
{
    public string OriginalUrl { get; set; } = string.Empty;

    public string CanonicalUrl { get; set; } = string.Empty;

    public string AffiliateUrl { get; set; } = string.Empty;

    public string? Title { get; set; }

    public decimal? Price { get; set; }

    public string? ImageUrl { get; set; }

    public MarketplaceType? Platform { get; set; }

    public string? SourceGroup { get; set; }
}

public class AffiliateLinkConverterResponseDto
{
    public bool Status { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public AffiliateLinkConverterResultDto? Data { get; set; }
}
