using TecFlow.Core.Enums;
using TecFlow.Database.Entity;

namespace TecFlow.Business.Service.LinkStrategies;

/// <summary>Monta registros de telemetria com campos obrigatórios e metadados de acesso.</summary>
public static class LinkClickLogFactory
{
    public static LinkClickLog CreateGeneration(
        Guid affiliateLinkId,
        Guid tenantId,
        string shopId,
        string originalUrl,
        string convertedUrl,
        MarketplaceType platformType,
        string? ipAddress,
        string? userAgent,
        string? referrerUrl) =>
        Create(
            affiliateLinkId,
            tenantId,
            shopId,
            originalUrl,
            convertedUrl,
            LinkClickLog.PlatformName(platformType),
            LinkClickLog.EventKindGeneration,
            ipAddress,
            userAgent,
            referrerUrl);

    public static LinkClickLog CreateClick(
        Guid affiliateLinkId,
        Guid tenantId,
        string shopId,
        string originalUrl,
        string convertedUrl,
        MarketplaceType platformType,
        string? ipAddress,
        string? userAgent,
        string? referrerUrl) =>
        Create(
            affiliateLinkId,
            tenantId,
            shopId,
            originalUrl,
            convertedUrl,
            LinkClickLog.PlatformName(platformType),
            LinkClickLog.EventKindClick,
            ipAddress,
            userAgent,
            referrerUrl);

    public static LinkClickLog Create(
        Guid affiliateLinkId,
        Guid tenantId,
        string shopId,
        string originalUrl,
        string convertedUrl,
        string platform,
        string eventKind,
        string? ipAddress,
        string? userAgent,
        string? referrerUrl)
    {
        if (affiliateLinkId == Guid.Empty)
        {
            throw new InvalidOperationException("AffiliateLinkId é obrigatório para persistir LinkClickLog.");
        }

        if (tenantId == Guid.Empty)
        {
            throw new InvalidOperationException("TenantId é obrigatório para persistir LinkClickLog.");
        }

        if (string.IsNullOrWhiteSpace(originalUrl) || string.IsNullOrWhiteSpace(convertedUrl))
        {
            throw new InvalidOperationException("OriginalUrl e ConvertedUrl são obrigatórios para persistir LinkClickLog.");
        }

        if (string.IsNullOrWhiteSpace(platform))
        {
            throw new InvalidOperationException("Platform é obrigatória para persistir LinkClickLog.");
        }

        var now = DateTime.UtcNow;
        var ua = string.IsNullOrWhiteSpace(userAgent)
            ? "desconhecido"
            : LinkClickTelemetryHelper.Truncate(userAgent, 512);

        return new LinkClickLog
        {
            AffiliateLinkId = affiliateLinkId,
            TenantId = tenantId,
            ShopId = LinkClickTelemetryHelper.Truncate(shopId, 128),
            OriginalUrl = LinkClickTelemetryHelper.Truncate(originalUrl, 2048),
            ConvertedUrl = LinkClickTelemetryHelper.Truncate(convertedUrl, 2048),
            Platform = LinkClickTelemetryHelper.Truncate(platform, 32),
            EventKind = string.IsNullOrWhiteSpace(eventKind) ? LinkClickLog.EventKindClick : eventKind.Trim(),
            CreatedAt = now,
            ClickedAt = now,
            IpAddress = LinkClickTelemetryHelper.MaskIpAddress(ipAddress),
            UserAgent = ua,
            DeviceType = LinkClickTelemetryHelper.DetectDeviceType(userAgent),
            ReferrerUrl = LinkClickTelemetryHelper.NormalizeReferrer(referrerUrl)
        };
    }
}
