using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Ofertio.API.Security;
using Ofertio.Business.Dto;
using Ofertio.Business.Integrations.Auth;
using Ofertio.Business.Interfaces.Services;

namespace Ofertio.API.Controllers;

/// <summary>Rotas públicas cadastradas no app do Mercado Livre (Redirect URI do OAuth e URL de notificações).</summary>
[ApiController]
[AllowAnonymous]
[Route("api/mercadolivre")]
public sealed class MercadoLivreController : ControllerBase
{
    private static readonly JsonSerializerOptions NotificationJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private readonly IIntegracaoLojaService _integracaoLojaService;
    private readonly ILogger<MercadoLivreController> _logger;
    private readonly IOAuthStateProtector? _stateProtector;
    private readonly IMemoryCache? _memoryCache;
    private readonly IGlobalIntegrationService? _globalIntegrationService;

    public MercadoLivreController(
        IIntegracaoLojaService integracaoLojaService,
        ILogger<MercadoLivreController> logger,
        IOAuthStateProtector? stateProtector = null,
        IMemoryCache? memoryCache = null,
        IGlobalIntegrationService? globalIntegrationService = null)
    {
        _integracaoLojaService = integracaoLojaService;
        _logger = logger;
        _stateProtector = stateProtector;
        _memoryCache = memoryCache;
        _globalIntegrationService = globalIntegrationService;
    }

    /// <summary>
    /// Redirect URI do OAuth 2.0: recebe <c>code</c> + <c>state</c> selado, grava os tokens e devolve o
    /// navegador para a origem onde o OAuth começou (<c>/marketplace-accounts</c> ou, no OAuth Master,
    /// <c>/master/integracoes-globais/mercado-livre</c>).
    /// </summary>
    [HttpGet("callback")]
    public async Task<IActionResult> CallbackAsync(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromQuery(Name = "error_description")] string? errorDescription,
        CancellationToken cancellationToken)
    {
        var outcome = await MercadoLivreOAuthCallbackFlow.CompleteAsync(
            HttpContext,
            new MarketplaceOAuthCallbackRequestDto
            {
                Code = code,
                State = state,
                Error = string.IsNullOrWhiteSpace(errorDescription) ? error : errorDescription
            },
            _stateProtector,
            _memoryCache,
            _integracaoLojaService,
            _logger,
            cancellationToken,
            _globalIntegrationService);

        return MercadoLivreOAuthCallbackFlow.IsBrowserReturnUrl(outcome.Result.ReturnUrl)
            ? Redirect(outcome.Result.ReturnUrl)
            : StatusCode(outcome.StatusCode, outcome.Result);
    }

    /// <summary>
    /// Notificações do Mercado Livre: responde 200 assim que lê o payload (o ML reenvia quando não recebe 200
    /// em até 500 ms e desativa tópicos com falhas recorrentes).
    /// </summary>
    [HttpPost("notifications")]
    public async Task<IActionResult> NotificationsAsync(CancellationToken cancellationToken)
    {
        var notification = await TryReadNotificationAsync(cancellationToken);
        if (notification is null)
        {
            _logger.LogWarning("Notificação Mercado Livre com payload vazio ou inválido.");
            return Ok();
        }

        _logger.LogInformation(
            "Notificação Mercado Livre recebida. Topic={Topic} Resource={Resource} UserId={UserId} ApplicationId={ApplicationId} Attempts={Attempts}",
            notification.Topic,
            notification.Resource,
            notification.UserId,
            notification.ApplicationId,
            notification.Attempts);
        return Ok();
    }

    private async Task<MercadoLivreNotificationDto?> TryReadNotificationAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await JsonSerializer.DeserializeAsync<MercadoLivreNotificationDto>(
                Request.Body,
                NotificationJsonOptions,
                cancellationToken);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
