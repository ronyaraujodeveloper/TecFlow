using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TecFlow.Business.Integrations.WhatsApp;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.WhatsApp;

namespace TecFlow.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/integrations/whatsapp")]
public sealed class WhatsAppWebhookController : ControllerBase
{
    private readonly IWhatsAppMessageProcessor _processor;
    private readonly EvolutionApiOptions _options;
    private readonly ILogger<WhatsAppWebhookController> _logger;

    public WhatsAppWebhookController(
        IWhatsAppMessageProcessor processor,
        IOptions<EvolutionApiOptions> options,
        ILogger<WhatsAppWebhookController> logger)
    {
        _processor = processor;
        _options = options.Value;
        _logger = logger;
    }

    [HttpPost("webhook")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ReceiveAsync(
        [FromBody] JsonElement payload,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
        {
            return Unauthorized();
        }

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

    private bool IsAuthorized()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return true;
        }

        var header = Request.Headers["apikey"].ToString();
        if (string.IsNullOrWhiteSpace(header))
        {
            header = Request.Headers["x-api-key"].ToString();
        }

        return string.Equals(header, _options.ApiKey, StringComparison.Ordinal);
    }
}
