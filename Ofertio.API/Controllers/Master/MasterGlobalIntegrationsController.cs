using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ofertio.Business.Dto;
using Ofertio.Business.Integrations.Auth;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Master;
using Ofertio.Core.Enums;
using Ofertio.Core.Security;

namespace Ofertio.API.Controllers.Master;

/// <summary>Integrações Globais: conexão Master com as APIs oficiais do Mercado Livre e do TikTok Shop.</summary>
[Route("api/master/integracoes-globais")]
public sealed class MasterGlobalIntegrationsController : MasterControllerBase
{
    private readonly IGlobalIntegrationService _globalIntegrations;
    private readonly IMarketplaceAuthService _marketplaceAuth;
    private readonly ILogger<MasterGlobalIntegrationsController> _logger;
    private readonly IOAuthStateProtector? _stateProtector;

    public MasterGlobalIntegrationsController(
        IGlobalIntegrationService globalIntegrations,
        IMarketplaceAuthService marketplaceAuth,
        ILogger<MasterGlobalIntegrationsController> logger,
        IOAuthStateProtector? stateProtector = null)
    {
        _globalIntegrations = globalIntegrations;
        _marketplaceAuth = marketplaceAuth;
        _logger = logger;
        _stateProtector = stateProtector;
    }

    [HttpGet("mercado-livre")]
    [Authorize(Policy = MasterPolicies.ManageGlobalKeys)]
    public async Task<ActionResult<GlobalIntegrationStatusDto>> GetMercadoLivreAsync(CancellationToken cancellationToken) =>
        Ok(await _globalIntegrations.GetMercadoLivreStatusAsync(cancellationToken));

    [HttpPut("mercado-livre/configuracoes")]
    [Authorize(Policy = MasterPolicies.ManageGlobalKeys)]
    public async Task<ActionResult<MasterOperationResultDto>> SaveMercadoLivreSettingsAsync(
        [FromBody] GlobalIntegrationSettingsDto request,
        CancellationToken cancellationToken) =>
        Result(await _globalIntegrations.SaveMercadoLivreSettingsAsync(Actor, request ?? new GlobalIntegrationSettingsDto(), cancellationToken));

    [HttpPost("mercado-livre/desconectar")]
    [Authorize(Policy = MasterPolicies.ManageGlobalKeys)]
    public async Task<ActionResult<MasterOperationResultDto>> DisconnectMercadoLivreAsync(CancellationToken cancellationToken) =>
        Result(await _globalIntegrations.DisconnectMercadoLivreAsync(Actor, cancellationToken));

    /// <summary>URL oficial de autorização com <c>state</c> selado marcado como Master (retorno ao Painel Master).</summary>
    [HttpGet("mercado-livre/authorize-url")]
    [Authorize(Policy = MasterPolicies.ManageGlobalKeys)]
    public ActionResult<MarketplaceAuthorizeUrlResponseDto> GetMercadoLivreAuthorizeUrl([FromQuery] string? redirectUri) =>
        BuildMasterAuthorizeUrl(MarketplaceType.MercadoLivre, redirectUri);

    [HttpGet("tiktok-shop")]
    [Authorize(Policy = MasterPolicies.ManageGlobalKeys)]
    public async Task<ActionResult<GlobalIntegrationStatusDto>> GetTikTokShopAsync(CancellationToken cancellationToken) =>
        Ok(await _globalIntegrations.GetTikTokShopStatusAsync(cancellationToken));

    [HttpPut("tiktok-shop/configuracoes")]
    [Authorize(Policy = MasterPolicies.ManageGlobalKeys)]
    public async Task<ActionResult<MasterOperationResultDto>> SaveTikTokShopSettingsAsync(
        [FromBody] GlobalIntegrationSettingsDto request,
        CancellationToken cancellationToken) =>
        Result(await _globalIntegrations.SaveTikTokShopSettingsAsync(Actor, request ?? new GlobalIntegrationSettingsDto(), cancellationToken));

    [HttpPost("tiktok-shop/desconectar")]
    [Authorize(Policy = MasterPolicies.ManageGlobalKeys)]
    public async Task<ActionResult<MasterOperationResultDto>> DisconnectTikTokShopAsync(CancellationToken cancellationToken) =>
        Result(await _globalIntegrations.DisconnectTikTokShopAsync(Actor, cancellationToken));

    /// <summary>URL oficial de autorização do app de criador com <c>state</c> selado marcado como Master.</summary>
    [HttpGet("tiktok-shop/authorize-url")]
    [Authorize(Policy = MasterPolicies.ManageGlobalKeys)]
    public ActionResult<MarketplaceAuthorizeUrlResponseDto> GetTikTokShopAuthorizeUrl([FromQuery] string? redirectUri) =>
        BuildMasterAuthorizeUrl(MarketplaceType.TikTokShop, redirectUri);

    private ActionResult<MarketplaceAuthorizeUrlResponseDto> BuildMasterAuthorizeUrl(MarketplaceType platform, string? redirectUri)
    {
        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            return Error("redirectUri é obrigatório.");
        }

        var actor = Actor;
        if (_stateProtector is null || actor.UserId <= 0)
        {
            return Error("Não foi possível selar a autorização OAuth. Faça login novamente.");
        }

        Guid? tenantId = Guid.TryParse(User.FindFirst(OfertioClaimTypes.TenantId)?.Value, out var tenant) ? tenant : null;
        var state = MarketplaceOAuthStateRules.Create(
            actor.UserId,
            tenantId,
            platform,
            "Integrações Globais",
            redirectUri) with { IsMaster = true };
        try
        {
            var url = _marketplaceAuth.GenerateAuthorizationUrl(platform, redirectUri, _stateProtector.Protect(state));
            return Ok(new MarketplaceAuthorizeUrlResponseDto
            {
                AuthorizeUrl = url,
                AuthorizationUrl = url,
                Marketplace = platform.ToString()
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "OAuth Master de {Platform} não configurado.", platform);
            return Error(ex.Message);
        }
    }
}
