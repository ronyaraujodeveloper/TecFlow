using System.Collections.Concurrent;

namespace TecFlow.Business.Service.WhatsApp;

public interface IWhatsAppBroadcastJobCoordinator
{
    CancellationToken Register(int campaignId, CancellationToken parentToken);

    void Unregister(int campaignId);

    bool Cancel(int campaignId);
}

public sealed class WhatsAppBroadcastJobCoordinator : IWhatsAppBroadcastJobCoordinator
{
    private readonly ConcurrentDictionary<int, CancellationTokenSource> _jobs = new();

    public CancellationToken Register(int campaignId, CancellationToken parentToken)
    {
        var linked = CancellationTokenSource.CreateLinkedTokenSource(parentToken);
        if (_jobs.TryRemove(campaignId, out var previous))
        {
            previous.Cancel();
            previous.Dispose();
        }

        _jobs[campaignId] = linked;
        return linked.Token;
    }

    public void Unregister(int campaignId)
    {
        if (_jobs.TryRemove(campaignId, out var source))
        {
            source.Dispose();
        }
    }

    public bool Cancel(int campaignId)
    {
        if (!_jobs.TryGetValue(campaignId, out var source))
        {
            return false;
        }

        source.Cancel();
        return true;
    }
}
