using System.Collections.Concurrent;
using TecFlow.Business.Interfaces.Telemetry;

namespace TecFlow.Observability;

public class TelemetryRecentErrorRecorder : ITelemetryRecentErrorRecorder
{
    private const int MaxEntries = 100;
    private readonly ConcurrentQueue<TelemetryErrorEntry> _entries = new();

    public void Record(string source, string message, string? detail = null)
    {
        _entries.Enqueue(new TelemetryErrorEntry(DateTimeOffset.UtcNow, source, message, detail));

        while (_entries.Count > MaxEntries && _entries.TryDequeue(out _))
        {
        }
    }

    public IReadOnlyList<TelemetryErrorEntry> GetRecent(int max = 50) =>
        _entries.Reverse().Take(Math.Clamp(max, 1, MaxEntries)).ToList();
}
