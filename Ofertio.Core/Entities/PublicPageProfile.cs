using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ofertio.Core.Abstractions;

namespace Ofertio.Core.Entities;

/// <summary>
/// Identidade do afiliado nas páginas públicas (foto). Um por usuário, independente do slug:
/// a troca de slug cria outro registro de <see cref="PublicConverterPage"/>, mas a foto continua a mesma.
/// </summary>
[Table("PublicPageProfiles")]
public class PublicPageProfile : BaseEntity, ITenantScopedEntity
{
    public int UserId { get; set; }

    public Guid TenantId { get; set; }

    /// <summary>Caminho público da foto (ex.: /uploads/avatars/{tenant}/{arquivo}.jpg). Nulo = sem foto.</summary>
    [MaxLength(300)]
    public string? AvatarUrl { get; set; }
}
