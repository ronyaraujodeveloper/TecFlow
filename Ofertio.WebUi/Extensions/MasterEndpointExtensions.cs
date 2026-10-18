using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Ofertio.Business.Dto;
using Ofertio.Core.Security;
using Ofertio.SharedUi.Services.Auth;
using Ofertio.WebUi.Services.Auth;

namespace Ofertio.WebUi.Extensions;

/// <summary>Troca de visão (master/cliente) e emulação de inquilino re-assinando o cookie de sessão.</summary>
public static class MasterEndpointExtensions
{
    private const string ImpersonatorNameClaim = "Ofertio:impersonator_name";
    private const string ImpersonatedTenantIdClaim = "Ofertio:impersonated_tenant_id";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly HashSet<string> SessionClaimTypes =
    [
        ClaimTypes.NameIdentifier,
        ClaimTypes.Name,
        ClaimTypes.Role,
        ClaimTypes.Email,
        OfertioClaimTypes.AccessToken,
        OfertioClaimTypes.TenantId,
        OfertioClaimTypes.ViewMode,
        OfertioClaimTypes.ImpersonatorId,
        OfertioClaimTypes.ImpersonatorEmail,
        OfertioClaimTypes.ImpersonatedTenantName,
        OfertioClaimTypes.ImpersonatorAccessToken,
        ImpersonatorNameClaim,
        ImpersonatedTenantIdClaim
    ];

    public static IEndpointRouteBuilder MapMasterEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/master");
        group.MapGet("/visao/{mode}", SwitchViewAsync);
        group.MapGet("/emular/sair", StopImpersonationAsync);
        group.MapGet("/emular/{tenantId:guid}", StartImpersonationAsync);
        return endpoints;
    }

    private static async Task<IResult> SwitchViewAsync(HttpContext httpContext, string mode)
    {
        var user = httpContext.User;
        if (!MasterAccess.IsMaster(user) || MasterAccess.IsImpersonating(user))
        {
            return Results.Redirect("/dashboard");
        }

        var view = string.Equals(mode, MasterAccess.ViewClient, StringComparison.OrdinalIgnoreCase)
            ? MasterAccess.ViewClient
            : MasterAccess.ViewMaster;
        var claims = KeepClaims(user);
        claims.Add(new Claim(OfertioClaimTypes.ViewMode, view));
        await ResignAsync(httpContext, claims);
        return Results.Redirect(view == MasterAccess.ViewMaster ? "/master" : "/dashboard");
    }

    private static async Task<IResult> StartImpersonationAsync(
        HttpContext httpContext,
        Guid tenantId,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory)
    {
        var user = httpContext.User;
        var masterToken = user.FindFirstValue(OfertioClaimTypes.AccessToken);
        if (!MasterAccess.IsMaster(user) || MasterAccess.IsImpersonating(user) || string.IsNullOrWhiteSpace(masterToken))
        {
            return Results.Redirect("/master/tenants?erro=" + Uri.EscapeDataString("Somente operadores master podem emular inquilinos."));
        }

        ImpersonationResultDto? result;
        string? error = null;
        try
        {
            var client = httpClientFactory.CreateClient("Orquestrador");
            using var request = new HttpRequestMessage(HttpMethod.Post, $"api/master/emular/{tenantId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", masterToken);
            using var response = await client.SendAsync(request, httpContext.RequestAborted);
            var content = await response.Content.ReadAsStringAsync(httpContext.RequestAborted);
            result = response.IsSuccessStatusCode ? JsonSerializer.Deserialize<ImpersonationResultDto>(content, JsonOptions) : null;
            if (result is null)
            {
                error = ReadDescricao(content) ?? $"Falha ao emular (HTTP {(int)response.StatusCode}).";
            }
        }
        catch (Exception ex)
        {
            loggerFactory.CreateLogger("Ofertio.WebUi.Master").LogError(ex, "Erro ao iniciar emulação do tenant {TenantId}.", tenantId);
            result = null;
            error = "Não foi possível falar com a API para emular o inquilino.";
        }

        if (result is not { Success: true } || string.IsNullOrWhiteSpace(result.AccessToken))
        {
            return Results.Redirect("/master/tenants?erro=" + Uri.EscapeDataString(error ?? result?.Message ?? "Falha ao emular."));
        }

        var claims = user.Claims
            .Where(claim => !SessionClaimTypes.Contains(claim.Type))
            .Select(claim => new Claim(claim.Type, claim.Value))
            .ToList();
        claims.Add(new Claim(ClaimTypes.NameIdentifier, result.TargetUserId.ToString()));
        claims.Add(new Claim(ClaimTypes.Name, result.TargetUserName));
        claims.Add(new Claim(OfertioClaimTypes.AccessToken, result.AccessToken));
        claims.AddRange(AuthCookieService.ClaimsFromToken(result.AccessToken));
        claims.Add(new Claim(OfertioClaimTypes.ImpersonatorAccessToken, masterToken));
        claims.Add(new Claim(OfertioClaimTypes.ImpersonatorId, user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0"));
        claims.Add(new Claim(ImpersonatorNameClaim, user.FindFirstValue(ClaimTypes.Name) ?? "Operador master"));
        claims.Add(new Claim(OfertioClaimTypes.ImpersonatorEmail, user.FindFirstValue(ClaimTypes.Email) ?? string.Empty));
        claims.Add(new Claim(OfertioClaimTypes.ImpersonatedTenantName, result.TenantName));
        claims.Add(new Claim(ImpersonatedTenantIdClaim, result.TenantId.ToString()));
        await ResignAsync(httpContext, claims);
        return Results.Redirect("/dashboard");
    }

    private static async Task<IResult> StopImpersonationAsync(
        HttpContext httpContext,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory)
    {
        var user = httpContext.User;
        var masterToken = user.FindFirstValue(OfertioClaimTypes.ImpersonatorAccessToken);
        if (string.IsNullOrWhiteSpace(masterToken))
        {
            return Results.Redirect("/dashboard");
        }

        var tenantId = user.FindFirstValue(ImpersonatedTenantIdClaim);
        var claims = user.Claims
            .Where(claim => !SessionClaimTypes.Contains(claim.Type))
            .Select(claim => new Claim(claim.Type, claim.Value))
            .ToList();
        claims.Add(new Claim(ClaimTypes.NameIdentifier, user.FindFirstValue(OfertioClaimTypes.ImpersonatorId) ?? "0"));
        claims.Add(new Claim(ClaimTypes.Name, user.FindFirstValue(ImpersonatorNameClaim) ?? "Operador master"));
        claims.Add(new Claim(OfertioClaimTypes.AccessToken, masterToken));
        claims.AddRange(AuthCookieService.ClaimsFromToken(masterToken));
        claims.Add(new Claim(OfertioClaimTypes.ViewMode, MasterAccess.ViewMaster));
        await ResignAsync(httpContext, claims);

        try
        {
            var client = httpClientFactory.CreateClient("Orquestrador");
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "api/master/emular/encerrar" + (Guid.TryParse(tenantId, out var id) ? $"?tenantId={id}" : string.Empty));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", masterToken);
            using var _ = await client.SendAsync(request, httpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            loggerFactory.CreateLogger("Ofertio.WebUi.Master").LogWarning(ex, "Falha ao registrar fim da emulação.");
        }

        return Results.Redirect("/master/tenants");
    }

    private static List<Claim> KeepClaims(ClaimsPrincipal user) =>
        user.Claims
            .Where(claim => claim.Type != OfertioClaimTypes.ViewMode)
            .Select(claim => new Claim(claim.Type, claim.Value))
            .ToList();

    private static async Task ResignAsync(HttpContext httpContext, List<Claim> claims)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = true });
    }

    private static string? ReadDescricao(string content)
    {
        try
        {
            using var document = JsonDocument.Parse(content);
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("descricao", out var value)
                && value.ValueKind == JsonValueKind.String
                    ? value.GetString()
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
