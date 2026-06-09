using TecFlow.Core.Enums;

namespace TecFlow.Business.Dto;

public class IntegracaoLojaDto
{
    public MarketplaceType PlatformType { get; set; }

    /// <summary>Authorization code retornado pelo callback OAuth do marketplace.</summary>
    public string AuthorizationCode { get; set; } = string.Empty;

    /// <summary>Identificador da loja na plataforma (ShopId).</summary>
    public string ShopId { get; set; } = string.Empty;

    /// <summary>Apelido amigável definido pelo usuário no painel.</summary>
    public string FriendlyName { get; set; } = string.Empty;
}
