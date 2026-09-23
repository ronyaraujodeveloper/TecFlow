using TecFlow.Business.Dto;
using TecFlow.Core.Enums;
using TecFlow.Database.Filter;

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

    Task<Guid> ResolveLinkGroupIdAsync(
        int userId,
        string originalUrl,
        MarketplaceType platformType,
        CancellationToken cancellationToken = default);

    Task<ShortLinkCreateResult> EnsureForStoreAsync(
        string destinationUrl,
        string originalUrl,
        MarketplaceType platformType,
        int userId,
        Guid tenantId,
        int integracaoLojaId,
        Guid linkGroupId,
        string? customNickname,
        CancellationToken cancellationToken = default);

    Task DeactivateUnselectedAccountsAsync(
        Guid linkGroupId,
        IReadOnlyCollection<int> selectedIntegracaoLojaIds,
        CancellationToken cancellationToken = default);
}

public interface ILinkClickTelemetryService
{
    Task RecordGenerationAsync(
        Guid affiliateLinkId,
        Guid tenantId,
        string shopId,
        string originalUrl,
        string convertedUrl,
        MarketplaceType platformType,
        string? ipAddress,
        string? userAgent,
        string? referrerUrl,
        CancellationToken cancellationToken = default);

    void EnqueueClickLog(
        Guid affiliateLinkId,
        string? ipAddress,
        string? userAgent,
        string? referrerUrl);
}

public interface IAffiliateLinkHistoryService
{
    Task<AffiliateLinkHistoryResponseDto> ListByUserAsync(
        int userId,
        AffiliateLinkFilter filter,
        CancellationToken cancellationToken = default);
}

public sealed class ShortLinkCreateResult
{
    public string PublicShortUrl { get; init; } = string.Empty;

    public Guid AffiliateLinkId { get; init; }

    public string ShortCode { get; init; } = string.Empty;
}
