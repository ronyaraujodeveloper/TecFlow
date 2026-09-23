using System.Text.Json.Serialization;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Dto;

/// <summary>Projeção segura de loja marketplace para a UI (sem tokens nem navegações EF).</summary>
public class MarketplaceAccountDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public Guid TenantId { get; set; }

    [JsonPropertyName("shopId")]
    [JsonConverter(typeof(FlexibleJsonStringConverter))]
    public string ShopId { get; set; } = string.Empty;

    /// <summary>ID de afiliado / Tracking ID (ex.: 18325850271).</summary>
    [JsonPropertyName("trackingId")]
    [JsonConverter(typeof(FlexibleJsonStringConverter))]
    public string TrackingId { get; set; } = string.Empty;

    [JsonPropertyName("affiliateTrackingId")]
    [JsonConverter(typeof(FlexibleJsonStringConverter))]
    public string AffiliateTrackingId { get; set; } = string.Empty;

    [JsonPropertyName("appKey")]
    [JsonConverter(typeof(FlexibleJsonStringConverter))]
    public string AppKey { get; set; } = string.Empty;

    public string FriendlyName { get; set; } = string.Empty;

    public string ShopName { get; set; } = string.Empty;

    public MarketplaceType PlatformType { get; set; }

    public DateTime ExpiresAt { get; set; }

    public MarketplaceIntegrationStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
}
