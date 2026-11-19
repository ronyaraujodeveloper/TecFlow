namespace Ofertio.Business.Dto;

public enum GroupInviteOutcome
{
    /// <summary>Convite obtido.</summary>
    Ok,

    /// <summary>O grupo não existe mais ou a conta/bot deixou de ser administrador.</summary>
    NotFound,

    /// <summary>Integração fora do ar ou erro passado: não dá para concluir nada sobre o grupo.</summary>
    Unavailable
}

/// <summary>Resultado de pedir o link de convite de um grupo ao WhatsApp (Evolution API) ou ao Telegram (bot).</summary>
public sealed record GroupInviteResult(GroupInviteOutcome Outcome, string? InviteUrl = null, int? MemberCount = null)
{
    public static GroupInviteResult Unavailable { get; } = new(GroupInviteOutcome.Unavailable);

    public static GroupInviteResult NotFound { get; } = new(GroupInviteOutcome.NotFound);
}
