using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using static TecFlow.WebUi.Extensions.AuthenticationExtensions;
using TecFlow.SharedUi.Models.Enums;
using TecFlow.SharedUi.Models.Requests;
using TecFlow.SharedUi.Services.Auth;

namespace TecFlow.WebUi.Extensions;

public static class AuthEndpointExtensions
{
    public static IEndpointRouteBuilder MapWebUiAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth");

        group.MapGet("/challenge/{provider}", ChallengeExternalProvider);
        group.MapGet("/finish-oauth", FinishOAuthAsync);
        group.MapGet("/logout", LogoutAsync);

        return endpoints;
    }

    private static IResult ChallengeExternalProvider(
        string provider,
        string platform,
        HttpContext httpContext,
        IOAuthProviderRegistry registry)
    {
        if (!Enum.TryParse<LoginPlatform>(platform, ignoreCase: true, out var loginPlatform))
        {
            return Results.BadRequest("Plataforma inválida.");
        }

        var authProvider = MapProviderKey(provider);
        if (authProvider is null || !registry.IsEnabled(authProvider.Value))
        {
            return Results.Redirect("/?oauth=not-configured");
        }

        var scheme = GetAuthenticationScheme(provider);
        if (scheme is null)
        {
            return Results.BadRequest("Provedor OAuth desconhecido.");
        }

        var properties = new AuthenticationProperties
        {
            RedirectUri = $"/auth/finish-oauth?platform={loginPlatform}&provider={authProvider}",
            Items =
            {
                ["login_platform"] = loginPlatform.ToString(),
                ["auth_provider"] = authProvider.ToString()!
            }
        };

        return Results.Challenge(properties, [scheme]);
    }

    private static async Task<IResult> FinishOAuthAsync(
        HttpContext httpContext,
        string platform,
        string provider,
        IOrquestradorAuthBridge authBridge,
        IAuthCookieService authCookie,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("TecFlow.WebUi.Auth");

        if (!Enum.TryParse<LoginPlatform>(platform, ignoreCase: true, out var loginPlatform)
            || !Enum.TryParse<AuthProvider>(provider, ignoreCase: true, out var authProvider))
        {
            return Results.Redirect("/");
        }

        var accessToken = await httpContext.GetTokenAsync("access_token");
        var idToken = await httpContext.GetTokenAsync("id_token");

        if (string.IsNullOrWhiteSpace(accessToken) && string.IsNullOrWhiteSpace(idToken))
        {
            logger.LogWarning("OAuth concluído sem tokens para {Provider}.", provider);
            return Results.Redirect("/?oauth=no-token");
        }

        var request = new PlatformAuthRequest
        {
            Provider = authProvider.ToString(),
            AccessToken = accessToken,
            IdToken = idToken
        };

        var result = await authBridge.LoginAsync(loginPlatform, request, httpContext.RequestAborted);
        if (!result.Success || result.Data is null)
        {
            var error = Uri.EscapeDataString(result.ErrorMessage ?? "Falha na autenticação.");
            return Results.Redirect($"/?oauth=failed&message={error}");
        }

        await authCookie.SignInFromAuthResponseAsync(result.Data, loginPlatform, authProvider, httpContext.RequestAborted);
        return Results.Redirect("/dashboard");
    }

    private static async Task<IResult> LogoutAsync(
        HttpContext httpContext,
        IAuthCookieService authCookie)
    {
        await authCookie.SignOutAsync(httpContext.RequestAborted);
        return Results.Redirect("/");
    }

    private static AuthProvider? MapProviderKey(string provider) => provider.ToLowerInvariant() switch
    {
        "google" => AuthProvider.Google,
        "facebook" => AuthProvider.Facebook,
        "apple" => AuthProvider.ICloud,
        _ => null
    };

    private static string? GetAuthenticationScheme(string provider) => provider.ToLowerInvariant() switch
    {
        "google" => GoogleDefaults.AuthenticationScheme,
        "facebook" => FacebookDefaults.AuthenticationScheme,
        "apple" => AppleScheme,
        _ => null
    };
}
