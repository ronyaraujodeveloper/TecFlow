using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.API.Security;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.WhatsApp;

namespace TecFlow.API.Controllers;

[ApiController]
[AllowAnonymous]
[WebhookSecurity]
[Route("api/v1/integrations/whatsapp")]
public sealed class WhatsAppWebhookController : ControllerBase
{
    private readonly IWhatsAppMessageProcessor _processor;
    private readonly ILogger<WhatsAppWebhookController> _logger;

    public WhatsAppWebhookController(
        IWhatsAppMessageProcessor processor,
        ILogger<WhatsAppWebhookController> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    [HttpPost("webhook")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ReceiveAsync(
        [FromBody] JsonElement payload,
        CancellationToken cancellationToken)
    {
        if (!WhatsAppBotRules.IsMessagesUpsert(payload))
        {
            return Ok(new { ignored = true });
        }

        var incoming = WhatsAppBotRules.TryParseIncoming(payload);
        if (incoming is not null && WhatsAppBotRules.ShouldIgnore(incoming))
        {
            return Ok(new { ignored = true, reason = "fromMe" });
        }

        try
        {
            await _processor.ProcessAsync(payload, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Webhook WhatsApp excedeu o limite de 3s.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha no webhook WhatsApp MESSAGES_UPSERT.");
        }

        return Ok(new { received = true });
    }
}
