using System.Globalization;
using TecFlow.Business.Dto;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database.Entity;

namespace TecFlow.Business.Mappings;

/// <summary>Projeção nula-segura de contas marketplace (registros antigos com colunas NULL).</summary>
public static class MarketplaceAccountMapper
{
    public static MarketplaceAccountDto ToDto(MarketplaceAccount? account, IntegracaoLoja? integration = null)
    {
        account ??= new MarketplaceAccount();

        _ = int.TryParse(account.UserId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedUserId);
        var shopId = Coalesce(account.ShopId, integration?.ShopId);
        var trackingId = Coalesce(account.TrackingId, account.AffiliateTrackingId, integration?.AffiliateTrackingId);
        var friendlyName = Coalesce(account.FriendlyName, account.ShopName, integration?.FriendlyName, shopId);
        var status = account.IsActive
            ? ResolveStatus(account.ExpiresAt, MarketplaceIntegrationStatus.Active)
            : MarketplaceIntegrationStatus.Inactive;

        return new MarketplaceAccountDto
        {
            Id = integration?.Id ?? account.Id,
            UserId = parsedUserId,
            TenantId = account.TenantId,
            ShopId = shopId,
            TrackingId = trackingId,
            AffiliateTrackingId = trackingId,
            AppKey = Coalesce(account.AppKey),
            FriendlyName = friendlyName,
            ShopName = Coalesce(account.ShopName, friendlyName, shopId),
            PlatformType = account.MarketplaceType,
            ExpiresAt = account.ExpiresAt,
            Status = status,
            CreatedAt = account.CreatedAt
        };
    }

    public static MarketplaceAccountDto ToDto(IntegracaoLoja? item)
    {
        item ??= new IntegracaoLoja();
        var shopId = Coalesce(item.ShopId);
        var trackingId = Coalesce(item.AffiliateTrackingId);
        var friendlyName = Coalesce(item.FriendlyName, shopId);

        return new MarketplaceAccountDto
        {
            Id = item.Id,
            UserId = item.UserId,
            TenantId = item.TenantId,
            ShopId = shopId,
            TrackingId = trackingId,
            AffiliateTrackingId = trackingId,
            AppKey = string.Empty,
            FriendlyName = friendlyName,
            ShopName = friendlyName,
            PlatformType = item.PlatformType,
            ExpiresAt = item.ExpiresAt,
            Status = ResolveStatus(item.ExpiresAt, item.Status),
            CreatedAt = item.CreatedAt
        };
    }

    public static ConvertLinkResponseDto ToConvertLinkResponse(MarketplaceAccount? account, IntegracaoLoja? integration = null)
    {
        var dto = ToDto(account, integration);
        return new ConvertLinkResponseDto
        {
            Status = true,
            Success = true,
            Descricao = "OK",
            Message = "OK",
            ShopId = dto.ShopId,
            TrackingId = dto.TrackingId
        };
    }

    private static MarketplaceIntegrationStatus ResolveStatus(
        DateTime expiresAt,
        MarketplaceIntegrationStatus currentStatus)
    {
        if (currentStatus == MarketplaceIntegrationStatus.Inactive)
        {
            return MarketplaceIntegrationStatus.Inactive;
        }

        if (expiresAt == default)
        {
            return currentStatus;
        }

        return expiresAt <= DateTime.UtcNow
            ? MarketplaceIntegrationStatus.Expired
            : MarketplaceIntegrationStatus.Active;
    }

    private static string Coalesce(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }
}
