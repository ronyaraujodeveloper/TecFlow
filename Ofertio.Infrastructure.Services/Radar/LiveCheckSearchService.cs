using Microsoft.Extensions.Logging;
using Ofertio.Business.Dto;
using Ofertio.Business.Integrations.MercadoLivre;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Groups;
using Ofertio.Business.Service.Radar;
using Ofertio.Core.Entities;
using Ofertio.Core.Enums;
using Ofertio.Database;

namespace Ofertio.Infrastructure.Services.Radar;

public sealed class LiveCheckSearchService : ILiveCheckSearchService
{
    private readonly IMercadoLivreApiService _mercadoLivre;
    private readonly IShopeeAffiliateOfferService _shopee;
    private readonly IAmazonPaApiService _amazon;
    private readonly IOfferValidationService _htmlValidation;
    private readonly AppDbContext _context;
    private readonly ILogger<LiveCheckSearchService> _logger;
    private readonly MercadoLivreAntiBotGuard _antiBotGuard;

    public LiveCheckSearchService(
        IMercadoLivreApiService mercadoLivre,
        IShopeeAffiliateOfferService shopee,
        IAmazonPaApiService amazon,
        IOfferValidationService htmlValidation,
        AppDbContext context,
        ILogger<LiveCheckSearchService> logger,
        MercadoLivreAntiBotGuard antiBotGuard)
    {
        _mercadoLivre = mercadoLivre;
        _shopee = shopee;
        _amazon = amazon;
        _htmlValidation = htmlValidation;
        _context = context;
        _logger = logger;
        _antiBotGuard = antiBotGuard;
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

        if (!string.IsNullOrWhiteSpace(snapshot.ResolvedProductUrl) && snapshot.ResolvedProductUrl.Length <= 1000)
        {
            entity.PrimaryProductUrl = snapshot.ResolvedProductUrl;
        }

        entity.OfferStatus = snapshot.Status;
        entity.IsAvailable = snapshot.IsAvailable;
        entity.LastValidatedAt = DateTime.UtcNow;
        if (snapshot.IsAvailable && snapshot.Price is > 0)
        {
            if (LiveSearchRules.IsCheaper(entity.ExtractedPrice, snapshot.Price)
                || snapshot.Status == GroupOfferStatuses.PrecoAlterado)
            {
                entity.OfferStatus = GroupOfferStatuses.PrecoAlterado;
            }

            entity.ValidatedPrice = snapshot.Price;
            if (OfferPriceTextRules.IsSignificantDivergence(entity.ExtractedPrice, snapshot.Price))
            {
                _logger.LogWarning(
                    "Divergência de preço corrigida. OfferId={OfferId} Plataforma={Platform} Fonte={Source} Salvo={Saved} Pagina={PagePrice}",
                    entity.Id,
                    snapshot.Platform ?? entity.PlatformType,
                    snapshot.Source,
                    entity.ExtractedPrice,
                    snapshot.Price);
                entity.ExtractedPrice = snapshot.Price;
                entity.OfferStatus = GroupOfferStatuses.PrecoAlterado;
            }
            else if (LiveSearchRules.IsCheaper(entity.ExtractedPrice, snapshot.Price))
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

        if (entity.IsAvailable)
        {
            entity.OfferStatus = OfferIntegrityRules.ApplyPriceSanity(
                entity.OfferStatus,
                entity.ProductName,
                entity.ValidatedPrice ?? entity.ExtractedPrice);
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
        var isMercadoLivre = (GroupOfferCaptureRules.DetectPlatform(url) ?? platform) == MarketplaceType.MercadoLivre;
        string? officialSku = null;
        if (isMercadoLivre
            && ProductSkuRules.TryExtract(url, platform, out var directKey, out var directSku)
            && directKey == nameof(MarketplaceType.MercadoLivre))
        {
            officialSku = directSku;
            var official = await _mercadoLivre.GetItemAsync(userId, directSku, cancellationToken);
            if (official is not null)
            {
                return FromApi(official, capturedPrice, null);
            }
        }

        if (isMercadoLivre && _antiBotGuard.IsBlocked)
        {
            _logger.LogWarning(
                "[ML Item] Página pública do Mercado Livre em pausa até {BlockedUntil:HH:mm} UTC após desafio anti-robô; oferta mantida sem raspar. Url={Url}",
                _antiBotGuard.BlockedUntil,
                url);
            return PendingAntiBot(url, capturedPrice);
        }

        var page = await _htmlValidation.ValidateProductPageStatusAsync(url ?? string.Empty, cancellationToken);
        if ((isMercadoLivre || MercadoLivreAvailabilityRules.IsMercadoLivreUrl(page.FinalUrl))
            && MercadoLivreAntiBotGuard.IsChallengePage(page.FinalUrl, page.Html))
        {
            _antiBotGuard.Trip();
            _logger.LogWarning(
                "[ML Item] Mercado Livre devolveu desafio anti-robô para {Url}; HTML descartado e página pública pausada por {Minutes} min. Conecte a conta Mercado Livre (OAuth) para validar pela API oficial.",
                url,
                MercadoLivreAntiBotGuard.Cooldown.TotalMinutes);
            return PendingAntiBot(url, capturedPrice);
        }

        var resolvedProductUrl = MercadoLivreShowcaseRules.TryResolveDirectProductUrl(page.FinalUrl, out var resolved)
            ? resolved
            : null;
        if (!page.IsAvailable)
        {
            return new OfficialOfferSnapshotDto
            {
                IsAvailable = false,
                Status = page.Status,
                Price = capturedPrice,
                Source = "Html",
                Platform = GroupOfferCaptureRules.DetectPlatform(page.FinalUrl ?? url) ?? platform,
                ResolvedProductUrl = resolvedProductUrl
            };
        }

        OfficialOfferSnapshotDto? api = null;
        if (ProductSkuRules.TryExtract(resolvedProductUrl ?? url, platform, out var platformKey, out var sku))
        {
            if (platformKey == nameof(MarketplaceType.MercadoLivre))
            {
                if (!string.Equals(sku, officialSku, StringComparison.OrdinalIgnoreCase))
                {
                    api = await _mercadoLivre.GetItemAsync(userId, sku, cancellationToken);
                }
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
            return FromApi(api, capturedPrice, resolvedProductUrl);
        }

        var html = await _htmlValidation.ValidateFromPageAsync(page, url ?? string.Empty, capturedPrice, cancellationToken);
        return new OfficialOfferSnapshotDto
        {
            IsAvailable = html.IsAvailable,
            Status = html.Status,
            Price = html.Price,
            ProductName = html.ProductName,
            ImageUrl = html.ImageUrl,
            Source = "Html",
            Platform = html.Platform ?? platform,
            ResolvedProductUrl = resolvedProductUrl
        };
    }

    private static OfficialOfferSnapshotDto FromApi(
        OfficialOfferSnapshotDto api,
        decimal? capturedPrice,
        string? resolvedProductUrl)
    {
        if (LiveSearchRules.IsCheaper(capturedPrice, api.Price))
        {
            api.Status = GroupOfferStatuses.PrecoAlterado;
        }

        api.ResolvedProductUrl ??= resolvedProductUrl;
        return api;
    }

    /// <summary>Sem API e com a página bloqueada: mantém a oferta em verificação, sem preço novo nem HTML do desafio.</summary>
    private static OfficialOfferSnapshotDto PendingAntiBot(string? url, decimal? capturedPrice) =>
        new()
        {
            IsAvailable = true,
            Status = GroupOfferStatuses.Verificando,
            Price = capturedPrice,
            Source = MercadoLivreAntiBotGuard.AntiBotSource,
            Platform = MarketplaceType.MercadoLivre,
            ResolvedProductUrl = MercadoLivreShowcaseRules.TryResolveDirectProductUrl(url, out var direct) ? direct : null
        };
}
