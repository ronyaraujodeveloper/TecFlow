using Ofertio.Core.Enums;

namespace Ofertio.Business.Dto;

public class UrlResolverDto
{
    public string InputUrl { get; set; } = string.Empty;
}

public class UrlResolverResultDto
{
    public string CanonicalUrl { get; set; } = string.Empty;

    public MarketplaceType? Platform { get; set; }

    public bool IsMarketplace { get; set; }
}

public class UrlResolverResponseDto
{
    public bool Status { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public UrlResolverResultDto? Data { get; set; }
}
