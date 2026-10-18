using System.Diagnostics;

namespace Ofertio.Observability;

public static class OfertioActivitySources
{
    public const string EngagementSourceName = "Ofertio.Engagement";
    public const string MarketplaceSourceName = "Ofertio.Marketplace";
    public const string ConciliationSourceName = "Ofertio.Conciliation";

    public static readonly ActivitySource Engagement = new(EngagementSourceName, "1.0.0");
    public static readonly ActivitySource Marketplace = new(MarketplaceSourceName, "1.0.0");
    public static readonly ActivitySource Conciliation = new(ConciliationSourceName, "1.0.0");
}
