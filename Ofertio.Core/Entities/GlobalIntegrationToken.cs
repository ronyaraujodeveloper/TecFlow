using Ofertio.Core.Enums;

namespace Ofertio.Core.Entities;

/// <summary>
/// Token OAuth Master de uma API oficial (ex.: Mercado Livre), autorizado no Painel Master e usado nas buscas
/// de todos os clientes. Escopo de sistema: não pertence a inquilino; tokens criptografados no banco.
/// </summary>
public class GlobalIntegrationToken : BaseEntity
{
    public MarketplaceType Provider { get; set; }

    public string AccessToken { get; set; } = string.Empty;

    public string? RefreshToken { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? RefreshExpiresAt { get; set; }

    public DateTime? LastRefreshAt { get; set; }

    /// <summary>Último erro de renovação automática (null quando o token está saudável).</summary>
    public string? LastError { get; set; }

    public DateTime? AuthorizedAt { get; set; }

    public int? AuthorizedByUserId { get; set; }

    public string? AuthorizedBy { get; set; }
}
