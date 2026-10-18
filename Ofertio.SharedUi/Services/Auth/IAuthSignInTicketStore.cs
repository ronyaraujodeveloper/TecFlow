using Ofertio.SharedUi.Models.Enums;
using Ofertio.SharedUi.Models.Responses;

namespace Ofertio.SharedUi.Services.Auth;

/// <summary>Ticket efêmero para concluir o cookie auth fora do circuito Blazor interativo.</summary>
public sealed class AuthSignInTicket
{
    public required AuthTokenResponse Response { get; init; }
    public required LoginPlatform Platform { get; init; }
    public required AuthProvider Provider { get; init; }
}

public interface IAuthSignInTicketStore
{
    string CreateTicket(AuthTokenResponse response, LoginPlatform platform, AuthProvider provider);

    AuthSignInTicket? ConsumeTicket(string ticketId);
}
