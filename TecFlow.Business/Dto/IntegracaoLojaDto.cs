using TecFlow.Core.Enums;
using System.Text.Json.Serialization;

namespace TecFlow.Business.Dto;

public class IntegracaoLojaDto
{
    [JsonPropertyName("platformType")]
    [JsonConverter(typeof(MarketplaceTypeJsonConverter))]
    public MarketplaceType PlatformType { get; set; } = MarketplaceType.Shopee;

    /// <summary>Authorization code retornado pelo callback OAuth do marketplace.</summary>
    [JsonPropertyName("authorizationCode")]
    [JsonConverter(typeof(FlexibleJsonStringConverter))]
    public string AuthorizationCode { get; set; } = string.Empty;

    /// <summary>Identificador da loja na plataforma (aceita string ou número JSON).</summary>
    [JsonPropertyName("shopId")]
    [JsonConverter(typeof(FlexibleJsonStringConverter))]
    public string ShopId { get; set; } = string.Empty;

    /// <summary>Apelido amigável definido pelo usuário no painel.</summary>
    [JsonPropertyName("friendlyName")]
    [JsonConverter(typeof(FlexibleJsonStringConverter))]
    public string FriendlyName { get; set; } = string.Empty;

    /// <summary>ID de afiliado / Tracking ID da Shopee (opcional; usado em sub_id do Universal Link).</summary>
    [JsonPropertyName("trackingId")]
    [JsonConverter(typeof(FlexibleJsonStringConverter))]
    public string? TrackingId { get; set; }
}
