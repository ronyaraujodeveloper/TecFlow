namespace TecFlow.Core.Entities;

/// <summary>
/// Conta corporativa de teste/dev da TecFlow (credenciais de app de desenvolvimento ou login de teste).
/// Escopo de sistema: não pertence a inquilino, não se mistura com MarketplaceAccounts e não alimenta o cofre global.
/// </summary>
public class SystemDeveloperAccount : BaseEntity
{
    public string Platform { get; set; } = string.Empty;

    public string Kind { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string Environment { get; set; } = string.Empty;

    /// <summary>AppId (Mercado Livre), AppKey (Shopee) ou Access Key (Amazon).</summary>
    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    /// <summary>Redirect URI (Mercado Livre) ou Partner Tag (Amazon).</summary>
    public string? Extra { get; set; }

    public string? TestUsername { get; set; }

    public string? TestPassword { get; set; }

    public string? Notes { get; set; }

    public DateTime? SecretExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? LastCheckAt { get; set; }

    public bool? LastCheckOk { get; set; }

    public string? LastCheckMessage { get; set; }

    public string? UpdatedBy { get; set; }
}
