using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace TecFlow.Tests.Helpers;

public sealed class EmptyAuthenticationStateProvider : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
        Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
}
