namespace Ofertio.Business.Dto;

/// <summary>Botão "entrar no grupo" como o visitante vê: sem o link de convite (o clique passa por /j/{id}).</summary>
public class PublicJoinButtonDto
{
    public int Id { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    /// <summary>Ex.: "1,3 mil membros". Vazio quando o afiliado escolheu não mostrar.</summary>
    public string MemberText { get; set; } = string.Empty;
}

/// <summary>Link de convite como o dono vê na tela de Minhas Páginas Públicas.</summary>
public class PublicPageJoinLinkDto
{
    public int Id { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string InviteUrl { get; set; } = string.Empty;
    /// <summary>Nome do grupo de origem; nulo quando o link foi colado à mão.</summary>
    public string? SourceGroupName { get; set; }
    /// <summary>Só links criados a partir de um grupo sincronizado podem ser conferidos pelo sistema.</summary>
    public bool IsVerifiable { get; set; }
    public bool ShowMemberCount { get; set; }
    public int? MemberCount { get; set; }
    public string Status { get; set; } = "Active";
    public string? StatusReason { get; set; }
    public DateTime? LastCheckedAt { get; set; }
    public int Clicks { get; set; }
}

/// <summary>Grupo sincronizado que o afiliado pode transformar em botão.</summary>
public class AvailableJoinGroupDto
{
    public string Channel { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ParticipantCount { get; set; }
    /// <summary>No WhatsApp só dá para gerar o convite onde a conta é administradora.</summary>
    public bool CanInvite { get; set; }
    public bool AlreadyUsed { get; set; }
}

public class JoinLinkCreateDto
{
    public string Channel { get; set; } = string.Empty;
    /// <summary>JID (WhatsApp) ou ChatId (Telegram) de um grupo sincronizado. Se vazio, usa <see cref="InviteUrl"/> (reserva manual).</summary>
    public string? GroupId { get; set; }
    public string? InviteUrl { get; set; }
    public string? Label { get; set; }
    public bool ShowMemberCount { get; set; }
}

public class JoinLinkUpdateDto
{
    public string? Label { get; set; }
    public bool ShowMemberCount { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PublicPageProfileDto
{
    public string? AvatarUrl { get; set; }
    /// <summary>Limite do plano (0 = ilimitado).</summary>
    public int MaxJoinLinks { get; set; }
    public bool CanAddJoinLink { get; set; }
    public List<PublicPageJoinLinkDto> JoinLinks { get; set; } = [];
    public List<AvailableJoinGroupDto> AvailableGroups { get; set; } = [];
}

public class PublicPageProfileResponseDto : ResponseDto
{
    public PublicPageProfileDto? Data { get; set; }
}
