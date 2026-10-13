using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.API.Controllers;

/// <summary>Webhook de cobranças ASAAS (header asaas-access-token = Payments:Asaas:WebhookToken).</summary>
[AllowAnonymous]
[ApiController]
[Route("api/webhooks/asaas")]
public sealed class AsaasWebhookController : ControllerBase
{
    private readonly IAsaasWebhookService _webhook;
    private readonly ILogger<AsaasWebhookController> _logger;

    public AsaasWebhookController(IAsaasWebhookService webhook, ILogger<AsaasWebhookController> logger)
    {
        _webhook = webhook;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveAsync([FromBody] AsaasWebhookPayloadDto payload, CancellationToken cancellationToken)
    {
        var token = Request.Headers["asaas-access-token"].ToString();
        if (!await _webhook.ValidateTokenAsync(token, cancellationToken))
        {
            return Unauthorized(new { status = false, descricao = "Token do webhook ASAAS inválido." });
        }

        try
        {
            var result = await _webhook.HandleAsync(payload ?? new AsaasWebhookPayloadDto(), cancellationToken);
            return Ok(new { received = true, result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao processar webhook ASAAS {Event}.", payload?.Event);
            return StatusCode(StatusCodes.Status500InternalServerError, new { received = false });
        }
    }
}
