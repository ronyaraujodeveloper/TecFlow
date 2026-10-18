using Ofertio.SharedUi.Services.State;

namespace Ofertio.SharedUi.Services.Http;

public class SessionAccessTokenProvider : IAccessTokenProvider
{
    private readonly ISessionStateService _sessionState;

    public SessionAccessTokenProvider(ISessionStateService sessionState)
    {
        _sessionState = sessionState;
    }

    public string? GetAccessToken() => _sessionState.AccessToken;
}
