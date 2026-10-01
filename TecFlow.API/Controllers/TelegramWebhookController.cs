using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TecFlow.API.Security;
using TecFlow.Business.Service.Security;
using TecFlow.Database;

namespace TecFlow.API.Controllers;

[ApiController]
[AllowAnonymous]
[WebhookSecurity]
[Route("api/v1/integrations/telegram")]
public sealed class TelegramWebhookController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<TelegramWebhookController> _logger;

    public TelegramWebhookController(AppDbContext context, ILogger<TelegramWebhookController> logger)
    {
        _context = context;
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
            if (userId is int currentUserId)
            {
                var integration = await _context.TelegramIntegrations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.UserId == currentUserId && item.IsActive, cancellationToken);
                if (integration is null)
                {
                    return Ok(new { ignored = true, reason = "no-integration" });
                }

                if (integration.UserId != currentUserId)
                {
                    throw new UnauthorizedAccessException();
                }

                IntegrationOwnershipGuard.EnsureOwner(integration.UserId, currentUserId);
            }

            _logger.LogDebug(
                "Webhook Telegram recebido. UserId={UserId} HasPayload={HasPayload}",
                userId,
                payload.ValueKind != JsonValueKind.Undefined);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }

        return Ok(new { received = true });
    }
}
