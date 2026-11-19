using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ofertio.Core.Abstractions;

namespace Ofertio.Core.Entities;

/// <summary>
/// Botão "entrar no grupo" mostrado na página pública depois que o visitante converte um link.
/// Criado a partir de um grupo sincronizado (WhatsApp ou Telegram) ou, como reserva, colando um link de convite.
/// </summary>
[Table("PublicPageJoinLinks")]
public class PublicPageJoinLink : BaseEntity, ITenantScopedEntity
{
    public int UserId { get; set; }

    public Guid TenantId { get; set; }

    /// <summary>Nome canônico do canal: WhatsApp ou Telegram.</summary>
    [Required]
    [MaxLength(16)]
    public string Channel { get; set; } = string.Empty;

    /// <summary>Nome do botão (ex.: o nome do grupo).</summary>
    [Required]
    [MaxLength(80)]
    public string Label { get; set; } = string.Empty;

    /// <summary>Link de convite que o visitante abre. Só este endereço, já validado, é usado no redirecionamento.</summary>
    [Required]
    [MaxLength(512)]
    public string InviteUrl { get; set; } = string.Empty;

    /// <summary>JID (WhatsApp) ou ChatId (Telegram) do grupo de origem. Nulo = link colado à mão, não verificável.</summary>
    [MaxLength(160)]
    public string? SourceGroupId { get; set; }

    /// <summary>Mostra o número de membros no botão (escolha feita ao criar o link).</summary>
    public bool ShowMemberCount { get; set; }

    public int? MemberCount { get; set; }

    /// <summary>Active, Inactive (grupo sumiu ou desligado) ou Unverified (integração fora do ar, sem poder conferir).</summary>
    [Required]
    [MaxLength(16)]
    public string Status { get; set; } = "Active";

    [MaxLength(200)]
    public string? StatusReason { get; set; }

    /// <summary>Falhas de conferência seguidas; a segunda desativa o link.</summary>
    public int FailedChecks { get; set; }

    public DateTime? LastCheckedAt { get; set; }

    public int SortOrder { get; set; }
}
