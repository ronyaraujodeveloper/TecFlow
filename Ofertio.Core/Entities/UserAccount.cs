using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ofertio.Core.Abstractions;

namespace Ofertio.Core.Entities;

[Table("Usuarios")]
public class UserAccount : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }

    public Tenant? Tenant { get; set; }

    [Required]
    [Column("Nome")]
    public required string Name { get; set; }

    [Required]
    public required string Email { get; set; }

    [Required]
    public required string PasswordHash { get; set; }

    [Column("Plano")]
    public string Plan { get; set; } = "Free";

    /// <summary>Papel no backoffice: TenantUser, SuperAdmin, SuporteTecnico ou FinanceiroSaaS.</summary>
    [MaxLength(32)]
    public string Role { get; set; } = "TenantUser";

    public bool IsActive { get; set; } = true;

    /// <summary>Número de celular/WhatsApp normalizado (11 dígitos, DDD + 9 + número).</summary>
    [Column("TelefoneWhatsApp")]
    public string? WhatsAppPhone { get; set; }

    public string? TikTokShopAccessToken { get; set; }
    public string? TikTokRefreshToken { get; set; }
    public DateTime? TikTokTokenExpiresAt { get; set; }

    /// <summary>Provedores sociais vinculados (AspNetUserLogins).</summary>
    public ICollection<UserExternalLogin> ExternalLogins { get; set; } = new List<UserExternalLogin>();
}
