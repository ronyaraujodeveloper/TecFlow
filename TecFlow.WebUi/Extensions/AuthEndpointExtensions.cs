using System.Net.Http.Json;
using System.Security.Claims;
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
        group.MapGet("/finish-link-oauth", FinishLinkOAuthAsync);
        group.MapGet("/complete-signin", CompleteSignInAsync);
        group.MapGet("/logout", LogoutAsync);

        return endpoints;
    }

    private static IResult ChallengeExternalProvider(
        string provider,
        string platform,
        bool link,
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
            return Results.Redirect(link ? "/minha-conta?link=not-configured" : "/?oauth=not-configured");
        }

        var scheme = GetAuthenticationScheme(provider);
        if (scheme is null)
        {
            return Results.BadRequest("Provedor OAuth desconhecido.");
        }

        if (link && httpContext.User.Identity?.IsAuthenticated != true)
        {
            return Results.Redirect("/?login=required");
        }

        var redirectUri = link
            ? $"/auth/finish-link-oauth?provider={provider.ToLowerInvariant()}"
            : $"/auth/finish-oauth?platform={loginPlatform}&provider={authProvider}";

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

    private static async Task<IResult> FinishLinkOAuthAsync(
        HttpContext httpContext,
        string provider,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("TecFlow.WebUi.Auth");

        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            return Results.Redirect("/?login=required");
        }

        var apiProvider = MapApiProviderName(provider);
        if (apiProvider is null)
        {
            return Results.Redirect("/minha-conta?link=failed&message=" + Uri.EscapeDataString("Provedor inválido."));
        }

        var accessToken = await httpContext.GetTokenAsync("access_token");
        var idToken = await httpContext.GetTokenAsync("id_token");

        if (string.IsNullOrWhiteSpace(accessToken) && string.IsNullOrWhiteSpace(idToken))
        {
            logger.LogWarning("Vinculação OAuth concluída sem tokens para {Provider}.", provider);
            return Results.Redirect("/minha-conta?link=failed&message=" + Uri.EscapeDataString("O provedor não devolveu token."));
        }

        var jwt = httpContext.User.FindFirstValue(TecFlow.Core.Security.TecFlowClaimTypes.AccessToken);
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
            var content = await response.Content.ReadAsStringAsync(httpContext.RequestAborted);

            if (response.IsSuccessStatusCode)
            {
                return Results.Redirect("/minha-conta?link=success");
            }

            logger.LogWarning(
                "Falha ao vincular provedor {Provider}. Status={StatusCode}. Body={Body}",
                provider,
                (int)response.StatusCode,
                content);

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
        var logger = loggerFactory.CreateLogger("TecFlow.WebUi.Auth");

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
