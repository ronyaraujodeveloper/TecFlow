namespace TecFlow.Business.Integrations.Shopee;

/// <summary>Compatibilidade do sandbox: delega à montagem de URL de comissão.</summary>
public static class ShopeeSandboxLinkBuilder
{
    public const string DefaultTrackingCode = ShopeeCommissionUrlBuilder.DefaultTrackingCode;
    public const string TrackingCodeQuery = ShopeeCommissionUrlBuilder.TrackingCodeQuery;
    public const string SubIdQuery = ShopeeCommissionUrlBuilder.SubIdQuery;

    public static string Build(
        string productUrl,
        string? trackingCode = null,
        string? subId = null,
        string? universalLink = null,
        string? deepLink = null) =>
        ShopeeCommissionUrlBuilder.Merge(productUrl, trackingCode, subId, universalLink, deepLink);
}
