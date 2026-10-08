namespace TecFlow.Business.Service.Radar;

public static class DataPurgeRules
{
    public static readonly TimeSpan MediaRetention = TimeSpan.FromDays(7);
    public static readonly TimeSpan MessageRetention = TimeSpan.FromDays(14);
    public static readonly TimeSpan DailyUtcTime = new(3, 0, 0);
    public const int DeleteBatchSize = 5000;
    public const int MaxDeleteRounds = 20;

    public static DateTime MediaCutoff(DateTime utcNow) => utcNow.ToUniversalTime().Subtract(MediaRetention);

    public static DateTime MessageCutoff(DateTime utcNow) => utcNow.ToUniversalTime().Subtract(MessageRetention);

    public static TimeSpan DelayUntilNextDailyUtc(DateTime utcNow)
    {
        var now = utcNow.ToUniversalTime();
        var next = now.Date.Add(DailyUtcTime);
        if (now >= next)
        {
            next = next.AddDays(1);
        }

        return next - now;
    }

    public static bool IsPngOrJpeg(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var ext = Path.GetExtension(path);
        return ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".webp", StringComparison.OrdinalIgnoreCase);
    }
}
