using System.Diagnostics;
using MassTransit;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Interfaces.Messaging;
using TecFlow.Business.Interfaces.Telemetry;
using TecFlow.Business.Messaging;
using TecFlow.Observability;

namespace TecFlow.Infrastructure.Services.Messaging.Consumers;

public class SocialMediaCommentConsumer : IConsumer<SocialMediaCommentReceivedEvent>
{
    private readonly ICommentKeywordTriageService _triage;
    private readonly IAffiliateLinkDeliveryNotifier _deliveryNotifier;
    private readonly ITecFlowBusinessMetrics _metrics;
    private readonly ITelemetryRecentErrorRecorder _errorRecorder;
    private readonly ILogger<SocialMediaCommentConsumer> _logger;

    public SocialMediaCommentConsumer(
        ICommentKeywordTriageService triage,
        IAffiliateLinkDeliveryNotifier deliveryNotifier,
        ITecFlowBusinessMetrics metrics,
        ITelemetryRecentErrorRecorder errorRecorder,
        ILogger<SocialMediaCommentConsumer> logger)
    {
        _triage = triage;
        _deliveryNotifier = deliveryNotifier;
        _metrics = metrics;
        _errorRecorder = errorRecorder;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SocialMediaCommentReceivedEvent> context)
    {
        using var activity = TecFlowActivitySources.Engagement.StartActivity("SocialMediaCommentConsumer.Process");
        activity?.SetTag("comment.event_id", context.Message.Id);
        activity?.SetTag("comment.post_id", context.Message.PostId);

        var message = context.Message;
        try
        {
        _logger.LogInformation(
            "Processando comentário {EventId} PostId={PostId} Platform={Platform} User={Username}",
            message.Id,
            message.PostId,
            message.Platform,
            message.Username);

        if (!_triage.IsEligibleForAffiliateLink(message.CommentText, out var matchedKeyword))
        {
            _logger.LogDebug(
                "Comentário {EventId} não elegível para link automático.",
                message.Id);
            _metrics.RecordCommentProcessed(success: true);
            return;
        }

        var delivery = new AffiliateLinkDeliveryRequestedEvent
        {
            CommentEventId = message.Id,
            PostId = message.PostId,
            Platform = message.Platform,
            Username = message.Username,
            MatchedKeyword = matchedKeyword ?? string.Empty,
            SimulatedAffiliateUrl = BuildSimulatedAffiliateUrl(message)
        };

        await _deliveryNotifier.NotifyDeliveryRequestedAsync(delivery, context.CancellationToken);
        _metrics.RecordCommentProcessed(success: true);
        _metrics.RecordAffiliateLinkSent(success: true);
        }
        catch (Exception ex)
        {
            _metrics.RecordCommentProcessed(success: false);
            _errorRecorder.Record("EngagementConsumer", ex.Message, message.Id.ToString());
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    private static string BuildSimulatedAffiliateUrl(SocialMediaCommentReceivedEvent message) =>
        $"https://tecflow.local/affiliate/{message.Platform}/{Uri.EscapeDataString(message.PostId)}?u={Uri.EscapeDataString(message.Username)}";
}
