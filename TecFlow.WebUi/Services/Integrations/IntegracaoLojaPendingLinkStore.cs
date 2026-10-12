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

    public string Create(MarketplaceType platformType, string friendlyName, string? callbackUri)
    {
        var ticketId = Guid.NewGuid().ToString("N");
        var payload = new IntegracaoLojaPendingLink
        {
            PlatformType = platformType,
            FriendlyName = friendlyName.Trim(),
            CallbackUri = string.IsNullOrWhiteSpace(callbackUri) ? null : callbackUri.Trim()
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

    public IntegracaoLojaPendingLink? Peek(string ticketId)
    {
        if (string.IsNullOrWhiteSpace(ticketId))
        {
            return null;
        }

        return _memoryCache.TryGetValue(BuildCacheKey(ticketId.Trim()), out IntegracaoLojaPendingLink? payload)
            ? payload
            : null;
    }

    public IntegracaoLojaPendingLink? Consume(string ticketId)
    {
        var payload = Peek(ticketId);
        if (payload is not null)
        {
            _memoryCache.Remove(BuildCacheKey(ticketId.Trim()));
        }

        return payload;
    }

    private static string BuildCacheKey(string ticketId) => $"{CacheKeyPrefix}{ticketId}";
}
