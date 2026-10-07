using Microsoft.Extensions.Logging;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.Infrastructure.Services.Groups;

public sealed class GroupOfferCaptureService : IGroupOfferCaptureService
{
    private readonly IOfferPipelineProcessor _pipeline;
    private readonly ILogger<GroupOfferCaptureService> _logger;

    public GroupOfferCaptureService(
        IOfferPipelineProcessor pipeline,
        ILogger<GroupOfferCaptureService> logger)
    {
        _pipeline = pipeline;
        _logger = logger;
    }

    public async Task CaptureAsync(GroupOfferCaptureRequest request, CancellationToken cancellationToken = default)
    {
        var saved = await _pipeline.ProcessAsync(request, requireLocalPhoto: false, cancellationToken);
        if (saved == 0)
        {
            _logger.LogDebug(
                "Nenhum pacote atômico persistido. UserId={UserId} Channel={Channel}",
                request.UserId,
                request.Channel);
        }
    }
}
