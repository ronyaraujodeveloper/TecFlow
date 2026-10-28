using Microsoft.Extensions.Caching.Memory;
using Serilog;
using Ofertio.Business.Dto;
using Ofertio.Business.Integrations.Auth;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Core.Enums;
using Ofertio.Infrastructure.Services.Integrations.Auth;

namespace Ofertio.API.Security;

/// <summary>
/// Conclui o OAuth do TikTok Shop (app de criador) sem sessão no navegador: o <c>state</c> selado identifica
/// usuário/tenant. Master grava o token global; cliente conecta a conta de criador na loja TikTok Shop.
/// </summary>
public static class TikTokShopOAuthCallbackFlow
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
            || state.Platform != MarketplaceType.TikTokShop)
        {
            logger.LogWarning("Callback TikTok Shop com state inválido, adulterado ou expirado.");
            return MercadoLivreOAuthCallbackFlow.Fail(StatusCodes.Status400BadRequest, null, MarketplaceOAuthStateRules.InvalidStateMessage);
        }

        var returnPath = MarketplaceOAuthStateRules.ReturnPathFor(state);
        if (!string.IsNullOrWhiteSpace(request!.Error))
        {
            return MercadoLivreOAuthCallbackFlow.Fail(StatusCodes.Status400BadRequest, state.CallbackUri, $"Autorização recusada: {request.Error.Trim()}", returnPath);
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return MercadoLivreOAuthCallbackFlow.Fail(StatusCodes.Status400BadRequest, state.CallbackUri, "Callback OAuth incompleto (code).", returnPath);
        }

        if (!MercadoLivreOAuthCallbackFlow.TryConsumeStateNonce(memoryCache, state))
        {
            return MercadoLivreOAuthCallbackFlow.Fail(StatusCodes.Status400BadRequest, state.CallbackUri, MarketplaceOAuthStateRules.ReplayedStateMessage, returnPath);
        }

        if (state.IsMaster)
        {
            return await CompleteMasterAsync(state, request.Code.Trim(), globalIntegrationService, logger, cancellationToken);
        }

        httpContext.User = MercadoLivreOAuthCallbackFlow.BuildStatePrincipal(state);
        try
        {
            var result = await integracaoLojaService.LinkAsync(
                state.UserId,
                new IntegracaoLojaDto
                {
                    PlatformType = MarketplaceType.TikTokShop,
                    AuthorizationCode = request.Code.Trim(),
                    FriendlyName = state.FriendlyName,
                    RedirectUri = state.CallbackUri
                },
                cancellationToken);
            if (!result.Status)
            {
                return MercadoLivreOAuthCallbackFlow.Fail(StatusCodes.Status400BadRequest, state.CallbackUri, result.Descricao, returnPath);
            }

            logger.LogInformation(
                "Conta de criador TikTok conectada via callback anônimo. UserId={UserId} TenantId={TenantId}",
                state.UserId,
                state.TenantId);
            return new MercadoLivreOAuthCallbackOutcome(StatusCodes.Status200OK, new MarketplaceOAuthCallbackResultDto
            {
                Success = true,
                Message = result.Descricao,
                ReturnUrl = MarketplaceOAuthStateRules.BuildReturnUrl(state.CallbackUri, success: true, path: returnPath)
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao salvar tokens do TikTok Shop no SQL Server");
            logger.LogError(ex, "Erro ao salvar tokens do TikTok Shop no SQL Server");
            return MercadoLivreOAuthCallbackFlow.Fail(StatusCodes.Status500InternalServerError, state.CallbackUri, MarketplaceAuthService.FormatSqlError(ex), returnPath);
        }
    }

    private static async Task<MercadoLivreOAuthCallbackOutcome> CompleteMasterAsync(
        MarketplaceOAuthState state,
        string code,
        IGlobalIntegrationService? globalIntegrationService,
        Microsoft.Extensions.Logging.ILogger logger,
        CancellationToken cancellationToken)
    {
        const string path = MarketplaceOAuthStateRules.MasterTikTokShopPath;
        if (globalIntegrationService is null)
        {
            return MercadoLivreOAuthCallbackFlow.Fail(StatusCodes.Status500InternalServerError, state.CallbackUri, "Serviço de Integrações Globais indisponível.", path);
        }

        try
        {
            var result = await globalIntegrationService.CompleteTikTokShopAuthorizationAsync(state.UserId, code, cancellationToken);
            if (!result.Success)
            {
                return MercadoLivreOAuthCallbackFlow.Fail(StatusCodes.Status400BadRequest, state.CallbackUri, result.Message, path);
            }

            logger.LogInformation("Token Master TikTok Shop gravado via callback. UserId={UserId}", state.UserId);
            return new MercadoLivreOAuthCallbackOutcome(StatusCodes.Status200OK, new MarketplaceOAuthCallbackResultDto
            {
                Success = true,
                Message = result.Message,
                ReturnUrl = MarketplaceOAuthStateRules.BuildReturnUrl(state.CallbackUri, success: true, path: path)
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao salvar o token Master do TikTok Shop no SQL Server");
            return MercadoLivreOAuthCallbackFlow.Fail(StatusCodes.Status500InternalServerError, state.CallbackUri, MarketplaceAuthService.FormatSqlError(ex), path);
        }
    }
}
