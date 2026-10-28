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
        CancellationToken cancellationToken,
        IGlobalIntegrationService? globalIntegrationService = null)
    {
        if (stateProtector is null
            || !stateProtector.TryUnprotect(request?.State, out var state)
            || state is null
            || state.Platform != MarketplaceType.MercadoLivre)
        {
            logger.LogWarning("Callback ML com state inválido, adulterado ou expirado.");
            return Fail(StatusCodes.Status400BadRequest, null, MarketplaceOAuthStateRules.InvalidStateMessage);
        }

        var returnPath = MarketplaceOAuthStateRules.ReturnPathFor(state);
        if (!string.IsNullOrWhiteSpace(request!.Error))
        {
            return Fail(StatusCodes.Status400BadRequest, state.CallbackUri, $"Autorização recusada: {request.Error.Trim()}", returnPath);
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return Fail(StatusCodes.Status400BadRequest, state.CallbackUri, "Callback OAuth incompleto (code).", returnPath);
        }

        if (!TryConsumeStateNonce(memoryCache, state))
        {
            return Fail(StatusCodes.Status400BadRequest, state.CallbackUri, MarketplaceOAuthStateRules.ReplayedStateMessage, returnPath);
        }

        if (state.IsMaster)
        {
            return await CompleteMasterAsync(state, request.Code.Trim(), globalIntegrationService, logger, cancellationToken);
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
                    RedirectUri = state.CallbackUri,
                    CodeVerifier = state.CodeVerifier
                },
                cancellationToken);
            if (!result.Status)
            {
                return Fail(StatusCodes.Status400BadRequest, state.CallbackUri, result.Descricao, returnPath);
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

    /// <summary>Token Master (Integrações Globais): grava o token global em vez de vincular uma loja do cliente.</summary>
    private static async Task<MercadoLivreOAuthCallbackOutcome> CompleteMasterAsync(
        MarketplaceOAuthState state,
        string code,
        IGlobalIntegrationService? globalIntegrationService,
        Microsoft.Extensions.Logging.ILogger logger,
        CancellationToken cancellationToken)
    {
        const string path = MarketplaceOAuthStateRules.MasterMercadoLivrePath;
        if (globalIntegrationService is null)
        {
            return Fail(StatusCodes.Status500InternalServerError, state.CallbackUri, "Serviço de Integrações Globais indisponível.", path);
        }

        try
        {
            var result = await globalIntegrationService.CompleteMercadoLivreAuthorizationAsync(
                state.UserId,
                code,
                state.CallbackUri,
                state.CodeVerifier,
                cancellationToken);
            if (!result.Success)
            {
                return Fail(StatusCodes.Status400BadRequest, state.CallbackUri, result.Message, path);
            }

            logger.LogInformation("Token Master ML gravado via callback. UserId={UserId}", state.UserId);
            return new MercadoLivreOAuthCallbackOutcome(StatusCodes.Status200OK, new MarketplaceOAuthCallbackResultDto
            {
                Success = true,
                Message = result.Message,
                ReturnUrl = MarketplaceOAuthStateRules.BuildReturnUrl(state.CallbackUri, success: true, path: path)
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao salvar o token Master do Mercado Livre no SQL Server");
            return Fail(StatusCodes.Status500InternalServerError, state.CallbackUri, MarketplaceAuthService.FormatSqlError(ex), path);
        }
    }

    /// <summary>Só redireciona o navegador para a origem absoluta do painel; sem ela a API responde JSON.</summary>
    public static bool IsBrowserReturnUrl(string? returnUrl) =>
        Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
        && MarketplaceOAuthStateRules.IsAllowedReturnPath(uri.AbsolutePath);

    internal static bool TryConsumeStateNonce(IMemoryCache? memoryCache, MarketplaceOAuthState state)
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

    internal static ClaimsPrincipal BuildStatePrincipal(MarketplaceOAuthState state)
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

    internal static MercadoLivreOAuthCallbackOutcome Fail(int statusCode, string? callbackUri, string? message, string? path = null)
    {
        var text = string.IsNullOrWhiteSpace(message) ? "Não foi possível vincular a loja." : message.Trim();
        return new MercadoLivreOAuthCallbackOutcome(statusCode, new MarketplaceOAuthCallbackResultDto
        {
            Success = false,
            Message = text,
            ReturnUrl = MarketplaceOAuthStateRules.BuildReturnUrl(callbackUri, success: false, text, path)
        });
    }
}
