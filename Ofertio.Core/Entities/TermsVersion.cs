using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ofertio.Core.Entities;

/// <summary>Versão publicada dos Termos de Uso ou da Política de Privacidade.</summary>
[Table("TermsVersions")]
public class TermsVersion : BaseEntity
{
    /// <summary>Terms ou Privacy.</summary>
    [MaxLength(16)]
    public string DocumentKind { get; set; } = "Terms";

    /// <summary>Número da versão, por exemplo 1.1.</summary>
    [MaxLength(16)]
    public string Version { get; set; } = "1.0";

    /// <summary>HTML sanitizado exibido na página pública.</summary>
    public string Content { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
