using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using Serilog;
using Ofertio.Business.Dto;
using Ofertio.Business.Integrations.Auth;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Core.Enums;
using Ofertio.Core.Security;
using Ofertio.Infrastructure.Services.Integrations.Auth;

namespace Ofertio.API.Security;

public sealed record MercadoLivreOAuthCallbackOutcome(int StatusCode, MarketplaceOAuthCallbackResultDto Result);

/// <summary>
/// Conclui o OAuth do Mercado Livre sem sessão no navegador: o <c>state</c> selado (AES-GCM) identifica
/// usuário/tenant, o nonce impede reuso e os tokens são gravados em MarketplaceAccounts.
/// </summary>
public static class MercadoLivreOAuthCallbackFlow
{
    public static async Task<MercadoLivreOAuthCallbackOutcome> CompleteAsync(
        HttpContext httpContext,
        MarketplaceOAuthCallbackRequestDto? request,
        IOAuthStateProtector? stateProtector,
        IMemoryCache? memoryCache,
        IIntegracaoLojaService integracaoLojaService,
        Microsoft.Extensions.Logging.ILogger logger,
        CancellationToken cancellationToken)
    {
        if (stateProtector is null
            || !stateProtector.TryUnprotect(request?.State, out var state)
            || state is null
            || state.Platform != MarketplaceType.MercadoLivre)
        {
            logger.LogWarning("Callback ML com state inválido, adulterado ou expirado.");
            return Fail(StatusCodes.Status400BadRequest, null, MarketplaceOAuthStateRules.InvalidStateMessage);
        }

        if (!string.IsNullOrWhiteSpace(request!.Error))
        {
            return Fail(StatusCodes.Status400BadRequest, state.CallbackUri, $"Autorização recusada: {request.Error.Trim()}");
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return Fail(StatusCodes.Status400BadRequest, state.CallbackUri, "Callback OAuth incompleto (code).");
        }

        if (!TryConsumeStateNonce(memoryCache, state))
        {
            return Fail(StatusCodes.Status400BadRequest, state.CallbackUri, MarketplaceOAuthStateRules.ReplayedStateMessage);
        }

        httpContext.User = BuildStatePrincipal(state);
        try
        {
            var result = await integracaoLojaService.LinkAsync(
                state.UserId,
                new IntegracaoLojaDto
                {
                    PlatformType = MarketplaceType.MercadoLivre,
                    AuthorizationCode = request.Code.Trim(),
                    FriendlyName = state.FriendlyName,
                    RedirectUri = state.CallbackUri
                },
                cancellationToken);
            if (!result.Status)
            {
                return Fail(StatusCodes.Status400BadRequest, state.CallbackUri, result.Descricao);
            }

            logger.LogInformation(
                "Tokens ML gravados via callback anônimo. UserId={UserId} TenantId={TenantId}",
                state.UserId,
                state.TenantId);
            return new MercadoLivreOAuthCallbackOutcome(StatusCodes.Status200OK, new MarketplaceOAuthCallbackResultDto
            {
                Success = true,
                Message = result.Descricao,
                ReturnUrl = MarketplaceOAuthStateRules.BuildReturnUrl(state.CallbackUri, success: true)
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao salvar tokens do Mercado Livre no SQL Server");
            logger.LogError(ex, "Erro ao salvar tokens do Mercado Livre no SQL Server");
            return Fail(StatusCodes.Status500InternalServerError, state.CallbackUri, MarketplaceAuthService.FormatSqlError(ex));
        }
    }

    /// <summary>Só redireciona o navegador para a origem absoluta do painel; sem ela a API responde JSON.</summary>
    public static bool IsBrowserReturnUrl(string? returnUrl) =>
        Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
        && uri.AbsolutePath.Equals(MarketplaceOAuthStateRules.AccountsPath, StringComparison.OrdinalIgnoreCase);

    private static bool TryConsumeStateNonce(IMemoryCache? memoryCache, MarketplaceOAuthState state)
    {
        if (memoryCache is null)
        {
            return true;
        }

        var key = "oauth-state-nonce:" + state.Nonce;
        if (memoryCache.TryGetValue(key, out _))
        {
            return false;
        }

        memoryCache.Set(key, true, state.ExpiresAt);
        return true;
    }

    private static ClaimsPrincipal BuildStatePrincipal(MarketplaceOAuthState state)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, state.UserId.ToString(System.Globalization.CultureInfo.InvariantCulture))
        };
        if (state.TenantId is { } tenantId)
        {
            claims.Add(new Claim(OfertioClaimTypes.TenantId, tenantId.ToString()));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "OAuthState"));
    }

    private static MercadoLivreOAuthCallbackOutcome Fail(int statusCode, string? callbackUri, string? message)
    {
        var text = string.IsNullOrWhiteSpace(message) ? "Não foi possível vincular a loja." : message.Trim();
        return new MercadoLivreOAuthCallbackOutcome(statusCode, new MarketplaceOAuthCallbackResultDto
        {
            Success = false,
            Message = text,
            ReturnUrl = MarketplaceOAuthStateRules.BuildReturnUrl(callbackUri, success: false, text)
        });
    }
}
