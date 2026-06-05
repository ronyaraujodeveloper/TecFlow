using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TecFlow.Core.Abstractions;

namespace TecFlow.Core.Entities;

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

    /// <summary>Número de celular/WhatsApp normalizado (11 dígitos, DDD + 9 + número).</summary>
    [Column("TelefoneWhatsApp")]
    public string? WhatsAppPhone { get; set; }

    public string? TikTokShopAccessToken { get; set; }
    public string? TikTokRefreshToken { get; set; }
    public DateTime? TikTokTokenExpiresAt { get; set; }
}
