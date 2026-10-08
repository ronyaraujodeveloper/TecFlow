using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Groups;
using TecFlow.Business.Service.Radar;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Radar;

public sealed class LiveCheckSearchService : ILiveCheckSearchService
{
    private readonly IMercadoLivreApiService _mercadoLivre;
    private readonly IShopeeAffiliateOfferService _shopee;
    private readonly IAmazonPaApiService _amazon;
    private readonly IOfferValidationService _htmlValidation;
    private readonly AppDbContext _context;

    public LiveCheckSearchService(
        IMercadoLivreApiService mercadoLivre,
        IShopeeAffiliateOfferService shopee,
        IAmazonPaApiService amazon,
        IOfferValidationService htmlValidation,
        AppDbContext context)
    {
        _mercadoLivre = mercadoLivre;
        _shopee = shopee;
        _amazon = amazon;
        _htmlValidation = htmlValidation;
        _context = context;
    }

    public async Task<OfficialOfferSnapshotDto> CheckAndPersistAsync(
        GroupCapturedMessage entity,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var url = string.IsNullOrWhiteSpace(entity.PrimaryProductUrl) ? entity.OriginalUrl : entity.PrimaryProductUrl;
        var snapshot = await CheckUrlAsync(
            userId,
            url,
            entity.PlatformType,
            entity.ValidatedPrice ?? entity.ExtractedPrice,
            cancellationToken);

        entity.OfferStatus = snapshot.Status;
        entity.IsAvailable = snapshot.IsAvailable;
        entity.LastValidatedAt = DateTime.UtcNow;
        if (snapshot.Price is > 0)
        {
            if (LiveSearchRules.IsCheaper(entity.ExtractedPrice, snapshot.Price)
                || snapshot.Status == GroupOfferStatuses.PrecoAlterado)
            {
                entity.OfferStatus = GroupOfferStatuses.PrecoAlterado;
            }

            entity.ValidatedPrice = snapshot.Price;
            if (LiveSearchRules.IsCheaper(entity.ExtractedPrice, snapshot.Price))
            {
                entity.ExtractedPrice = snapshot.Price;
            }
        }

        if (!string.IsNullOrWhiteSpace(snapshot.CouponCode))
        {
            entity.CouponCode = snapshot.CouponCode.Length <= 64 ? snapshot.CouponCode : snapshot.CouponCode[..64];
        }

        if (!string.IsNullOrWhiteSpace(snapshot.ProductName))
        {
            entity.ProductName = snapshot.ProductName;
        }

        if (!string.IsNullOrWhiteSpace(snapshot.ImageUrl)
            && !ProductImageStorageRules.IsLocalProductImage(snapshot.ImageUrl)
            && string.IsNullOrWhiteSpace(entity.ProductImageUrl))
        {
            entity.ProductImageUrl = snapshot.ImageUrl;
        }

        if (snapshot.Platform is { } platform)
        {
            entity.PlatformType = platform;
        }

        entity.Touch();
        await _context.SaveChangesAsync(cancellationToken);
        return snapshot;
    }

    public async Task<OfficialOfferSnapshotDto> CheckUrlAsync(
        int userId,
        string? url,
        MarketplaceType? platform,
        decimal? capturedPrice,
        CancellationToken cancellationToken = default)
    {
        OfficialOfferSnapshotDto? api = null;
        if (ProductSkuRules.TryExtract(url, platform, out var platformKey, out var sku))
        {
            if (platformKey == nameof(MarketplaceType.MercadoLivre))
            {
                api = await _mercadoLivre.GetItemAsync(sku, cancellationToken);
            }
            else if (platformKey == nameof(MarketplaceType.Shopee))
            {
                var parts = sku.Split(':', 2);
                if (parts.Length == 2)
                {
                    api = await _shopee.GetProductOfferAsync(userId, parts[0], parts[1], cancellationToken);
                }
            }
            else if (platformKey == nameof(MarketplaceType.Amazon))
            {
                api = await _amazon.GetItemAsync(userId, sku, cancellationToken);
            }
        }

        if (api is not null)
        {
            if (LiveSearchRules.IsCheaper(capturedPrice, api.Price))
            {
                api.Status = GroupOfferStatuses.PrecoAlterado;
            }

            return api;
        }

        var html = await _htmlValidation.ValidateAsync(url ?? string.Empty, capturedPrice, cancellationToken);
        return new OfficialOfferSnapshotDto
        {
            IsAvailable = html.IsAvailable,
            Status = html.Status,
            Price = html.Price,
            ProductName = html.ProductName,
            ImageUrl = html.ImageUrl,
            Source = "Html",
            Platform = html.Platform ?? platform
        };
    }
}
