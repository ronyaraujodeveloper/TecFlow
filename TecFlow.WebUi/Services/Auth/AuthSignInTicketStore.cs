using Microsoft.Extensions.Caching.Memory;
using TecFlow.SharedUi.Models.Enums;
using TecFlow.SharedUi.Models.Responses;
using TecFlow.SharedUi.Services.Auth;

namespace TecFlow.WebUi.Services.Auth;

public class AuthSignInTicketStore : IAuthSignInTicketStore
{
    private static readonly TimeSpan TicketLifetime = TimeSpan.FromMinutes(2);
    private const string CacheKeyPrefix = "auth-signin-ticket:";

    private readonly IMemoryCache _memoryCache;

    public AuthSignInTicketStore(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public string CreateTicket(AuthTokenResponse response, LoginPlatform platform, AuthProvider provider)
    {
        var ticketId = Guid.NewGuid().ToString("N");
        var payload = new AuthSignInTicket
        {
            Response = response,
            Platform = platform,
            Provider = provider
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

    public AuthSignInTicket? ConsumeTicket(string ticketId)
    {
        if (string.IsNullOrWhiteSpace(ticketId))
        {
            return null;
        }

        var cacheKey = BuildCacheKey(ticketId.Trim());
        if (!_memoryCache.TryGetValue(cacheKey, out AuthSignInTicket? payload))
        {
            return null;
        }

        _memoryCache.Remove(cacheKey);
        return payload;
    }

    private static string BuildCacheKey(string ticketId) => $"{CacheKeyPrefix}{ticketId}";
}
