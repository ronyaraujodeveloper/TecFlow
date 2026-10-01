using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.API.Security;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.API.Controllers;

[ApiController]
[AllowAnonymous]
[WebhookSecurity]
[Route("api/v1/integrations/telegram")]
public sealed class TelegramWebhookController : ControllerBase
{
    private readonly ITelegramMessageProcessor _processor;
    private readonly ILogger<TelegramWebhookController> _logger;

    public TelegramWebhookController(
        ITelegramMessageProcessor processor,
        ILogger<TelegramWebhookController> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    [HttpPost("webhook")]
    [HttpPost("webhook/{userId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ReceiveAsync(
        int? userId,
        [FromBody] JsonElement payload,
        CancellationToken cancellationToken)
    {
        try
        {
            await _processor.ProcessAsync(userId, payload, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Webhook Telegram excedeu o limite de 2s.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha no webhook Telegram.");
        }

        return Ok(new { received = true });
    }
}
