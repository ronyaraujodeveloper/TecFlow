using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using static Ofertio.WebUi.Extensions.AuthenticationExtensions;
using Ofertio.SharedUi.Models.Enums;
using Ofertio.SharedUi.Models.Requests;
using Ofertio.SharedUi.Services.Auth;

namespace Ofertio.WebUi.Extensions;

public static class AuthEndpointExtensions
{
    public static IEndpointRouteBuilder MapWebUiAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth");

        group.MapGet("/challenge/{provider}", ChallengeExternalProvider);
        group.MapGet("/finish-oauth", FinishOAuthAsync);
        group.MapGet("/finish-link-oauth", FinishLinkOAuthAsync);
        group.MapGet("/complete-signin", CompleteSignInAsync);
        group.MapGet("/logout", LogoutAsync);

        return endpoints;
    }

    private static async Task<IResult> ChallengeExternalProvider(
        string provider,
        HttpContext httpContext,
        IOAuthProviderRegistry registry,
        IAuthenticationSchemeProvider schemes,
        string? platform = null,
        bool link = false)
    {
        var authProvider = MapProviderKey(provider);
        var scheme = GetAuthenticationScheme(provider);
        var configured = authProvider is not null
            && registry.IsEnabled(authProvider.Value)
            && scheme is not null
            && await schemes.GetSchemeAsync(scheme) is not null;

        if (!configured)
        {
            return Results.Redirect(link
                ? "/minha-conta?link=not-configured"
                : "/?oauth=not-configured");
        }

        if (link && httpContext.User.Identity?.IsAuthenticated != true)
        {
            return Results.Redirect("/?login=required");
        }

        var loginPlatform = ResolvePortalPlatform(platform);
        var redirectUri = link
            ? $"/auth/finish-link-oauth?provider={provider.ToLowerInvariant()}"
            : $"/auth/finish-oauth?provider={authProvider}";

        var properties = new AuthenticationProperties
        {
            RedirectUri = redirectUri,
            Items =
            {
                ["login_platform"] = loginPlatform.ToString(),
                ["auth_provider"] = authProvider.ToString()!,
                ["link_mode"] = link ? "true" : "false"
            }
        };

        return Results.Challenge(properties, [scheme!]);
    }

    private static async Task<IResult> FinishOAuthAsync(
        HttpContext httpContext,
        string provider,
        IOrquestradorAuthBridge authBridge,
        IAuthCookieService authCookie,
        ILoggerFactory loggerFactory,
        string? platform = null)
    {
        var logger = loggerFactory.CreateLogger("Ofertio.WebUi.Auth");

        if (!Enum.TryParse<AuthProvider>(provider, ignoreCase: true, out var authProvider))
        {
            return Results.Redirect("/");
        }

        var loginPlatform = ResolvePortalPlatform(platform);

        try
        {
            var (accessToken, idToken) = await TakeExternalTokensAsync(httpContext);

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
                logger.LogWarning(
                    "Falha OAuth para {Provider}/{Platform}: {Error}",
                    provider,
                    platform,
                    result.ErrorMessage);
                var error = Uri.EscapeDataString(result.ErrorMessage ?? "Falha na autenticação.");
                return Results.Redirect($"/?oauth=failed&message={error}");
            }

            await authCookie.SignInFromAuthResponseAsync(result.Data, loginPlatform, authProvider, httpContext.RequestAborted);
            return Results.Redirect("/dashboard");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro inesperado no callback OAuth {Provider}/{Platform}.", provider, platform);
            return Results.Redirect("/?oauth=failed&message=" + Uri.EscapeDataString("Erro inesperado ao concluir login."));
        }
    }

    private static async Task<IResult> FinishLinkOAuthAsync(
        HttpContext httpContext,
        string provider,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("Ofertio.WebUi.Auth");

        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            return Results.Redirect("/?login=required");
        }

        var apiProvider = MapApiProviderName(provider);
        if (apiProvider is null)
        {
            return Results.Redirect("/minha-conta?link=failed&message=" + Uri.EscapeDataString("Provedor inválido."));
        }

        var (accessToken, idToken) = await TakeExternalTokensAsync(httpContext);

        if (string.IsNullOrWhiteSpace(accessToken) && string.IsNullOrWhiteSpace(idToken))
        {
            logger.LogWarning("Vinculação OAuth concluída sem tokens para {Provider}.", provider);
            return Results.Redirect("/minha-conta?link=failed&message=" + Uri.EscapeDataString("O provedor não devolveu token."));
        }

        var jwt = httpContext.User.FindFirstValue(Ofertio.Core.Security.OfertioClaimTypes.AccessToken);
        if (string.IsNullOrWhiteSpace(jwt))
        {
            return Results.Redirect("/minha-conta?link=failed&message=" + Uri.EscapeDataString("Sessão inválida. Faça login novamente."));
        }

        try
        {
            var client = httpClientFactory.CreateClient("Orquestrador");
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/providers/vincular");
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
            request.Content = JsonContent.Create(new
            {
                Provider = apiProvider,
                AccessToken = accessToken,
                IdToken = idToken
            });

            using var response = await client.SendAsync(request, httpContext.RequestAborted);

            if (response.IsSuccessStatusCode)
            {
                return Results.Redirect("/minha-conta?link=success");
            }

            logger.LogWarning(
                "Falha ao vincular provedor {Provider}. Status={StatusCode}",
                provider,
                (int)response.StatusCode);

            return Results.Redirect("/minha-conta?link=failed&message=" + Uri.EscapeDataString("Não foi possível vincular a conta."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao vincular provedor {Provider}.", provider);
            return Results.Redirect("/minha-conta?link=failed&message=" + Uri.EscapeDataString("Erro inesperado ao vincular conta."));
        }
    }

    private static async Task<IResult> CompleteSignInAsync(
        string ticket,
        IAuthSignInTicketStore ticketStore,
        IAuthCookieService authCookie,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("Ofertio.WebUi.Auth");

        var payload = ticketStore.ConsumeTicket(ticket);
        if (payload is null)
        {
            logger.LogWarning("Ticket de sign-in inválido ou expirado.");
            return Results.Redirect("/?login=failed&message=" + Uri.EscapeDataString("Sessão de login expirada. Tente novamente."));
        }

        try
        {
            await authCookie.SignInFromAuthResponseAsync(
                payload.Response,
                payload.Platform,
                payload.Provider);

            return Results.Redirect("/dashboard");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao concluir sign-in por ticket.");
            return Results.Redirect("/?login=failed&message=" + Uri.EscapeDataString("Não foi possível concluir o login."));
        }
    }

    private static async Task<IResult> LogoutAsync(
        HttpContext httpContext,
        IAuthCookieService authCookie)
    {
        await authCookie.SignOutAsync(httpContext.RequestAborted);
        return Results.Redirect("/");
    }

    private static LoginPlatform ResolvePortalPlatform(string? platform) =>
        Enum.TryParse<LoginPlatform>(platform, ignoreCase: true, out var loginPlatform)
            ? loginPlatform
            : LoginPlatform.TikTok;

    private static async Task<(string? AccessToken, string? IdToken)> TakeExternalTokensAsync(HttpContext httpContext)
    {
        var result = await httpContext.AuthenticateAsync(ExternalScheme);
        string? accessToken = null;
        string? idToken = null;
        if (result.Succeeded)
        {
            accessToken = result.Properties?.GetTokenValue("access_token");
            idToken = result.Properties?.GetTokenValue("id_token");
        }

        await httpContext.SignOutAsync(ExternalScheme);
        return (accessToken, idToken);
    }

    private static AuthProvider? MapProviderKey(string provider) => provider.ToLowerInvariant() switch
    {
        "google" => AuthProvider.Google,
        "facebook" => AuthProvider.Facebook,
        "apple" => AuthProvider.ICloud,
        _ => null
    };

    private static string? MapApiProviderName(string provider) => provider.ToLowerInvariant() switch
    {
        "google" => "Google",
        "facebook" => "Facebook",
        "apple" => "Apple",
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
