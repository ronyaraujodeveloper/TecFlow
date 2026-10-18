using System.Text.Json.Serialization;
using Ofertio.Core.Enums;

namespace Ofertio.Business.Dto;

/// <summary>Projeção segura de loja marketplace para a UI (sem tokens nem navegações EF).</summary>
public class MarketplaceAccountDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public Guid TenantId { get; set; }

    [JsonPropertyName("shopId")]
    [JsonConverter(typeof(FlexibleJsonStringConverter))]
    public string ShopId { get; set; } = string.Empty;

    /// <summary>ID de afiliado / Tracking ID (ex.: 6512300000).</summary>
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

    public bool RequiresTrackingIdCorrection { get; set; }

    /// <summary>True quando a loja possui token OAuth real (não o placeholder do Universal Link).</summary>
    public bool HasOAuthToken { get; set; }
}
