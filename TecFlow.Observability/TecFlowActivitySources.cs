using System.Diagnostics;

namespace TecFlow.Observability;

public static class TecFlowActivitySources
{
    public const string EngagementSourceName = "TecFlow.Engagement";
    public const string MarketplaceSourceName = "TecFlow.Marketplace";
    public const string ConciliationSourceName = "TecFlow.Conciliation";

    public static readonly ActivitySource Engagement = new(EngagementSourceName, "1.0.0");
    public static readonly ActivitySource Marketplace = new(MarketplaceSourceName, "1.0.0");
    public static readonly ActivitySource Conciliation = new(ConciliationSourceName, "1.0.0");
}
