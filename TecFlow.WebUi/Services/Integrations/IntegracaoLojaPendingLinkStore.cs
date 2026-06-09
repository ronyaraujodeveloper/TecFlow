using Microsoft.Extensions.Caching.Memory;
using TecFlow.Core.Enums;
using TecFlow.SharedUi.Services.Integrations;

namespace TecFlow.WebUi.Services.Integrations;

public class IntegracaoLojaPendingLinkStore : IIntegracaoLojaPendingLinkStore
{
    private static readonly TimeSpan TicketLifetime = TimeSpan.FromMinutes(15);
    private const string CacheKeyPrefix = "integracao-loja-pending:";

    private readonly IMemoryCache _memoryCache;

    public IntegracaoLojaPendingLinkStore(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public string Create(MarketplaceType platformType, string friendlyName)
    {
        var ticketId = Guid.NewGuid().ToString("N");
        var payload = new IntegracaoLojaPendingLink
        {
            PlatformType = platformType,
            FriendlyName = friendlyName.Trim()
        };

        _memoryCache.Set(
            BuildCacheKey(ticketId),
            payload,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TicketLifetime
            });

        return ticketId;
    }

    public IntegracaoLojaPendingLink? Consume(string ticketId)
    {
        if (string.IsNullOrWhiteSpace(ticketId))
        {
            return null;
        }

        var cacheKey = BuildCacheKey(ticketId.Trim());
        if (!_memoryCache.TryGetValue(cacheKey, out IntegracaoLojaPendingLink? payload))
        {
            return null;
        }

        _memoryCache.Remove(cacheKey);
        return payload;
    }

    private static string BuildCacheKey(string ticketId) => $"{CacheKeyPrefix}{ticketId}";
}
