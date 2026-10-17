namespace TecFlow.Business.Service.Groups;

public sealed class OfferRevalidationOptions
{
    public const string SectionName = "OfferRevalidation";

    public bool Enabled { get; set; } = true;

    public double IntervalHours { get; set; } = 2;

    public int InitialDelayMinutes { get; set; } = 5;

    public int BatchSize { get; set; } = 200;

    public int DelayBetweenChecksMs { get; set; } = 1000;

    public TimeSpan Interval => TimeSpan.FromHours(Math.Clamp(IntervalHours, 0.25, 168));

    public TimeSpan InitialDelay => TimeSpan.FromMinutes(Math.Clamp(InitialDelayMinutes, 0, 1440));

    public int EffectiveBatchSize => Math.Clamp(BatchSize, 1, 1000);

    public TimeSpan DelayBetweenChecks => TimeSpan.FromMilliseconds(Math.Clamp(DelayBetweenChecksMs, 0, 60_000));

    public static bool IsDue(DateTime? lastValidatedAt, DateTime utcNow, TimeSpan interval) =>
        lastValidatedAt is null || lastValidatedAt.Value <= utcNow - interval;
}
