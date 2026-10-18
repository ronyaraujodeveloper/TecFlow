namespace Ofertio.Core.Entities;

/// <summary>Parâmetro global (chave de configuração .NET, ex.: Integrations:MercadoLivre:AppId) com valor criptografado.</summary>
public class SystemSetting : BaseEntity
{
    public string Key { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public bool IsSecret { get; set; }

    public string? UpdatedBy { get; set; }
}
