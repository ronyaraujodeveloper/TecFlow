using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TecFlow.Core.Abstractions;

namespace TecFlow.Core.Entities;

/// <summary>Página pública de conversão do afiliado. Troca de slug versiona o registro (IsActive).</summary>
[Table("PublicConverterPages")]
public class PublicConverterPage : BaseEntity, ITenantScopedEntity
{
    /// <summary>Identidade estável da página (sobrevive à troca de slug).</summary>
    public Guid PublicCode { get; set; } = Guid.NewGuid();

    public int UserId { get; set; }

    public Guid TenantId { get; set; }

    [Required]
    [MaxLength(64)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(128)]
    public string? DisplayName { get; set; }

    public bool IsActive { get; set; } = true;
}
