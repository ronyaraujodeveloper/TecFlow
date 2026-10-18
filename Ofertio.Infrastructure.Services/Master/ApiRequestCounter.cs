using System.Collections.Concurrent;
using Ofertio.Business.Interfaces.Services;

namespace Ofertio.Infrastructure.Services.Master;

/// <summary>Contador em memória de requisições da API em baldes horários (janela de 24h).</summary>
public sealed class ApiRequestCounter : IApiRequestCounter
{
    private const int WindowHours = 24;

    private readonly ConcurrentDictionary<long, long> _buckets = new();
    private readonly Func<DateTime> _clock;

    public ApiRequestCounter()
        : this(() => DateTime.UtcNow)
    {
    }

    public ApiRequestCounter(Func<DateTime> clock)
    {
        _clock = clock;
    }

    public void Increment()
    {
        var hour = HourKey(_clock());
        _buckets.AddOrUpdate(hour, 1, (_, current) => current + 1);
        if (_buckets.Count > WindowHours + 2)
        {
            Prune(hour);
        }
    }

    public long CountLast24Hours()
    {
        var current = HourKey(_clock());
        return _buckets.Where(item => item.Key > current - WindowHours).Sum(item => item.Value);
    }

    private void Prune(long currentHour)
    {
        foreach (var key in _buckets.Keys.Where(key => key <= currentHour - WindowHours))
        {
            _buckets.TryRemove(key, out _);
        }
    }

    private static long HourKey(DateTime utc) => utc.Ticks / TimeSpan.TicksPerHour;
}
