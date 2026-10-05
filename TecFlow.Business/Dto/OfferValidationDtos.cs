using TecFlow.Core.Entities;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Dto;

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
}
