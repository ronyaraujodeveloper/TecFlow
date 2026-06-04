namespace TecFlow.Business.Interfaces.Telemetry;

public interface ITelemetryRecentErrorRecorder
{
    void Record(string source, string message, string? detail = null);
    IReadOnlyList<TelemetryErrorEntry> GetRecent(int max = 50);
}

public record TelemetryErrorEntry(
    DateTimeOffset OccurredAtUtc,
    string Source,
    string Message,
    string? Detail);
