using Ofertio.Core.Entities;
using Ofertio.Core.Enums;

namespace Ofertio.Business.Dto;

public sealed class OfferPageStatusDto
{
    public bool IsAvailable { get; init; } = true;

    public string Status { get; init; } = GroupOfferStatuses.Verificando;

    public string? Html { get; init; }

    public string? FinalUrl { get; init; }
}

public sealed class OfferValidationResultDto
{
    public string Status { get; init; } = GroupOfferStatuses.Verificando;

    public bool IsAvailable { get; init; } = true;

    public decimal? Price { get; init; }

    public string? ImageUrl { get; init; }

    public string? ProductName { get; init; }

    public MarketplaceType? Platform { get; init; }

    public string? FinalUrl { get; init; }
}

public sealed class OfferRevalidationSummaryDto
{
    public int Checked { get; set; }

    public int Deactivated { get; set; }

    public int Suspicious { get; set; }

    public int Failed { get; set; }
}
