using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using TecFlow.Business.Dto;
using TecFlow.Core.Enums;
using TecFlow.Core.Security;
using TecFlow.SharedUi.Services.Integrations;

namespace TecFlow.WebUi.Extensions;

public static class IntegracoesEndpointExtensions
{
    public static IEndpointRouteBuilder MapIntegracoesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/integracoes");

        group.MapGet("/oauth/start", StartMarketplaceOAuthAsync);
        group.MapGet("/oauth/callback", FinishMarketplaceOAuthAsync);

        return endpoints;
    }

    private static async Task<IResult> StartMarketplaceOAuthAsync(
        HttpContext httpContext,
        MarketplaceType platform,
        string friendlyName,
        IIntegracaoLojaPendingLinkStore pendingLinkStore,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("TecFlow.WebUi.Integracoes");

        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            return Results.Redirect("/?login=required");
        }

        if (string.IsNullOrWhiteSpace(friendlyName))
        {
            return Results.Redirect("/minhas-lojas?link=failed&message=" + Uri.EscapeDataString("Informe um apelido para a loja."));
        }

        var redirectUri = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/integracoes/oauth/callback";
        var ticket = pendingLinkStore.Create(platform, friendlyName, redirectUri);

        try
        {
            var client = httpClientFactory.CreateClient("Orquestrador");
            var url =
                $"api/marketplace-auth/authorize-url?type={(int)platform}&redirectUri={Uri.EscapeDataString(redirectUri)}&state={Uri.EscapeDataString(ticket)}";

            using var response = await client.GetAsync(url, httpContext.RequestAborted);
            var content = await response.Content.ReadAsStringAsync(httpContext.RequestAborted);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Falha ao gerar URL OAuth. Status={StatusCode}", (int)response.StatusCode);
                return Results.Redirect("/minhas-lojas?link=failed&message=" + Uri.EscapeDataString("Não foi possível iniciar OAuth."));
            }

            var payload = JsonSerializer.Deserialize<AuthorizationUrlResponse>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (string.IsNullOrWhiteSpace(payload?.AuthorizationUrl))
            {
                return Results.Redirect("/minhas-lojas?link=failed&message=" + Uri.EscapeDataString("URL OAuth inválida."));
            }

            return Results.Redirect(payload.AuthorizationUrl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao iniciar OAuth de marketplace.");
            return Results.Redirect("/minhas-lojas?link=failed&message=" + Uri.EscapeDataString("Erro inesperado ao iniciar OAuth."));
        }
    }

    private static async Task<IResult> FinishMarketplaceOAuthAsync(
        HttpContext httpContext,
        IIntegracaoLojaPendingLinkStore pendingLinkStore,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("TecFlow.WebUi.Integracoes");

        var query = httpContext.Request.Query;
        var state = query["state"].ToString();
        var code = query["code"].ToString();
        var shopId = query["shop_id"].ToString();

        if (string.IsNullOrWhiteSpace(shopId))
        {
            shopId = query["shopId"].ToString();
        }

        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            var bounce = ResolveOriginBounce(httpContext, pendingLinkStore.Peek(state));
            return bounce is null ? Results.Redirect("/?login=required") : Results.Redirect(bounce);
        }

        var pending = pendingLinkStore.Consume(state);
        if (pending is null)
        {
            return Results.Redirect("/minhas-lojas?link=failed&message=" + Uri.EscapeDataString("Sessão OAuth expirada. Tente novamente."));
        }

        if (!string.IsNullOrWhiteSpace(query["error"].ToString()))
        {
            var reason = FirstNonEmpty(query["error_description"].ToString(), query["error"].ToString());
            return Results.Redirect("/minhas-lojas?link=failed&message=" + Uri.EscapeDataString($"Autorização recusada: {reason}"));
        }

        var requiresShopId = pending.PlatformType != MarketplaceType.MercadoLivre;
        if (string.IsNullOrWhiteSpace(code) || (requiresShopId && string.IsNullOrWhiteSpace(shopId)))
        {
            return Results.Redirect("/minhas-lojas?link=failed&message=" + Uri.EscapeDataString(
                requiresShopId ? "Callback OAuth incompleto (code/shopId)." : "Callback OAuth incompleto (code)."));
        }

        var jwt = httpContext.User.FindFirstValue(TecFlowClaimTypes.AccessToken);
        if (string.IsNullOrWhiteSpace(jwt))
        {
            return Results.Redirect("/minhas-lojas?link=failed&message=" + Uri.EscapeDataString("Sessão inválida. Faça login novamente."));
        }

        try
        {
            var client = httpClientFactory.CreateClient("Orquestrador");
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/integracoes/vincular");
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
            request.Content = JsonContent.Create(new IntegracaoLojaDto
            {
                PlatformType = pending.PlatformType,
                AuthorizationCode = code,
                ShopId = shopId,
                FriendlyName = pending.FriendlyName,
                RedirectUri = pending.CallbackUri
            });

            using var response = await client.SendAsync(request, httpContext.RequestAborted);
            var content = await response.Content.ReadAsStringAsync(httpContext.RequestAborted);

            if (response.IsSuccessStatusCode)
            {
                return Results.Redirect("/minhas-lojas?link=success");
            }

            logger.LogWarning(
                "Falha ao vincular loja via OAuth. Status={StatusCode}. Body={Body}",
                (int)response.StatusCode,
                content);

            var apiMessage = TryReadDescricao(content);
            return Results.Redirect("/minhas-lojas?link=failed&message=" + Uri.EscapeDataString(
                string.IsNullOrWhiteSpace(apiMessage) ? "Não foi possível vincular a loja." : apiMessage));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao concluir vinculação OAuth de marketplace.");
            return Results.Redirect("/minhas-lojas?link=failed&message=" + Uri.EscapeDataString("Erro inesperado ao vincular loja."));
        }
    }

    /// <summary>
    /// O callback pode chegar por outro host (ex.: túnel HTTPS cadastrado no Mercado Livre) sem o cookie
    /// de sessão; nesse caso reenvia code/state para o callback da origem onde o OAuth foi iniciado.
    /// </summary>
    private static string? ResolveOriginBounce(HttpContext httpContext, IntegracaoLojaPendingLink? pending)
    {
        if (pending is null
            || !Uri.TryCreate(pending.CallbackUri, UriKind.Absolute, out var origin)
            || string.Equals(origin.Authority, httpContext.Request.Host.Value, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return origin.GetLeftPart(UriPartial.Path) + httpContext.Request.QueryString.Value;
    }

    private static string? TryReadDescricao(string content)
    {
        try
        {
            using var document = JsonDocument.Parse(content);
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("descricao", out var descricao)
                && descricao.ValueKind == JsonValueKind.String
                    ? descricao.GetString()
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

    private sealed class AuthorizationUrlResponse
    {
        public string AuthorizationUrl { get; set; } = string.Empty;
    }
}
