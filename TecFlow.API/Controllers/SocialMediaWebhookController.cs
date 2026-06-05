using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Messaging;
using TecFlow.Business.Messaging;

namespace TecFlow.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/webhooks/social-media")]
public class SocialMediaWebhookController : ControllerBase
{
    private readonly IEngagementEventPublisher _publisher;
    private readonly ILogger<SocialMediaWebhookController> _logger;

    public SocialMediaWebhookController(
        IEngagementEventPublisher publisher,
        ILogger<SocialMediaWebhookController> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    /// <summary>
    /// Recebe comentário de rede social e publica na fila RabbitMQ (resposta rápida 202).
    /// </summary>
    [HttpPost("comments")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReceiveCommentAsync(
        [FromBody] SocialMediaCommentWebhookRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PostId) ||
            string.IsNullOrWhiteSpace(request.CommentText) ||
            string.IsNullOrWhiteSpace(request.Username))
        {
            return BadRequest(new { message = "PostId, Username e CommentText são obrigatórios." });
        }

        var message = new SocialMediaCommentReceivedEvent
        {
            Id = Guid.NewGuid(),
            PostId = request.PostId.Trim(),
            Platform = request.Platform,
            Username = request.Username.Trim(),
            CommentText = request.CommentText.Trim(),
            Timestamp = request.Timestamp ?? DateTimeOffset.UtcNow,
            OwnerId = request.OwnerId,
            Metadata = request.Metadata is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(request.Metadata, StringComparer.OrdinalIgnoreCase)
        };

        await _publisher.PublishCommentReceivedAsync(message, cancellationToken);

        _logger.LogInformation(
            "Comentário {EventId} aceito para fila (PostId={PostId}, Platform={Platform}).",
            message.Id,
            message.PostId,
            message.Platform);

        return Accepted(new
        {
            message = "Comentário enfileirado para processamento.",
            eventId = message.Id,
            queue = "social-media-comment-received"
        });
    }
}
