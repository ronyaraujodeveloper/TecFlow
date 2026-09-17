using TecFlow.Core.Enums;
using System.Text.Json.Serialization;

namespace TecFlow.Business.Dto;

public class IntegracaoLojaDto
{
    public MarketplaceType PlatformType { get; set; }

    /// <summary>Authorization code retornado pelo callback OAuth do marketplace.</summary>
    [JsonPropertyName("authorizationCode")]
    public string AuthorizationCode { get; set; } = string.Empty;

    /// <summary>Identificador da loja na plataforma (ShopId numérico na Shopee).</summary>
    [JsonPropertyName("shopId")]
    public string ShopId { get; set; } = string.Empty;

    /// <summary>Apelido amigável definido pelo usuário no painel.</summary>
    public string FriendlyName { get; set; } = string.Empty;
}
