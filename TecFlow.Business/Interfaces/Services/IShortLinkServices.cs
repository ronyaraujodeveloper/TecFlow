using TecFlow.Business.Dto;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Interfaces.Services;

public interface IShortLinkService
{
    Task<(string PublicShortUrl, Guid AffiliateLinkId)> CreateShortLinkAsync(
        string destinationUrl,
        string originalUrl,
        MarketplaceType platformType,
        int userId,
        Guid tenantId,
        int? integracaoLojaId,
        string? customNickname,
        CancellationToken cancellationToken = default);
}

public interface ILinkClickTelemetryService
{
    void EnqueueClickLog(
        Guid affiliateLinkId,
        string? ipAddress,
        string? userAgent,
        string? referrerUrl);
}

public sealed class ShortLinkCreateResult
{
    public string PublicShortUrl { get; init; } = string.Empty;

    public Guid AffiliateLinkId { get; init; }

    public string ShortCode { get; init; } = string.Empty;
}
