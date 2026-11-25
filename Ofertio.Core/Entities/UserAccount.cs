using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
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

    /// <summary>
    /// Nunca sai em resposta JSON, mesmo que a entidade seja devolvida por engano.
    /// Sem o modificador <c>required</c>: o System.Text.Json recusa required + JsonIgnore até na serialização.
    /// O [Required] mantém a coluna NOT NULL.
    /// </summary>
    [Required]
    [JsonIgnore]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("Plano")]
    public string Plan { get; set; } = "Free";

    /// <summary>Papel no backoffice: TenantUser, SuperAdmin, SuporteTecnico ou FinanceiroSaaS.</summary>
    [MaxLength(32)]
    public string Role { get; set; } = "TenantUser";

    public bool IsActive { get; set; } = true;

    /// <summary>Número de celular/WhatsApp normalizado (11 dígitos, DDD + 9 + número).</summary>
    [Column("TelefoneWhatsApp")]
    public string? WhatsAppPhone { get; set; }

    /// <summary>UF do cliente; define o fuso dos horários na tela. Vazio = Brasília.</summary>
    [Column("Estado")]
    [MaxLength(2)]
    public string? StateCode { get; set; }

    /// <summary>Versão dos Termos de Uso aceita por este usuário. Nula até o primeiro aceite.</summary>
    [MaxLength(16)]
    public string? TermsAcceptedVersion { get; set; }

    public DateTime? TermsAcceptedAt { get; set; }

    [MaxLength(64)]
    public string? TermsAcceptedIp { get; set; }

    [JsonIgnore]
    public string? TikTokShopAccessToken { get; set; }

    [JsonIgnore]
    public string? TikTokRefreshToken { get; set; }
    public DateTime? TikTokTokenExpiresAt { get; set; }

    /// <summary>Provedores sociais vinculados (AspNetUserLogins).</summary>
    public ICollection<UserExternalLogin> ExternalLogins { get; set; } = new List<UserExternalLogin>();
}
