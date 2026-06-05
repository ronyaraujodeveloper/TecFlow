using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Advertising;

public class AdvertisingProductService : IAdvertisingProductService
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    private readonly AppDbContext _db;
    private readonly ILogger<AdvertisingProductService> _logger;

    public AdvertisingProductService(AppDbContext db, ILogger<AdvertisingProductService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<GlobalAdvertisingProduct> CreateGlobalProductAsync(
        int ownerId,
        GlobalAdvertisingProductDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.FriendlyName))
        {
            throw new ArgumentException("FriendlyName é obrigatório.");
        }

        var product = new GlobalAdvertisingProduct
        {
            GlobalProductUid = Guid.NewGuid(),
            FriendlyName = dto.FriendlyName.Trim(),
            Description = dto.Description?.Trim() ?? string.Empty,
            GlobalCategory = dto.GlobalCategory?.Trim() ?? "Geral",
            MainImageUrl = dto.MainImageUrl?.Trim(),
            AveragePrice = dto.AveragePrice,
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow
        };

        _db.GlobalAdvertisingProducts.Add(product);
        await _db.SaveChangesAsync(cancellationToken);

        foreach (var linkDto in dto.MarketplaceLinks)
        {
            if (string.IsNullOrWhiteSpace(linkDto.OriginalProductUrl))
            {
                continue;
            }

            var tracking = NormalizeTrackingParameters(linkDto.CustomTrackingParameters, product.GlobalProductUid);
            var generated = await BuildAffiliateLinkAsync(
                linkDto.MarketplaceType,
                linkDto.OriginalProductUrl.Trim(),
                linkDto.PlatformProductId,
                tracking,
                cancellationToken);

            _db.MarketplaceAffiliateLinks.Add(new MarketplaceAffiliateLink
            {
                GlobalProductId = product.Id,
                MarketplaceType = linkDto.MarketplaceType,
                OriginalProductUrl = linkDto.OriginalProductUrl.Trim(),
                PlatformProductId = linkDto.PlatformProductId?.Trim(),
                GeneratedAffiliateLink = generated,
                CustomTrackingParameters = tracking,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Produto global de propaganda {Uid} criado com {LinkCount} vínculos marketplace.",
            product.GlobalProductUid,
            dto.MarketplaceLinks.Count);

        return await _db.GlobalAdvertisingProducts
            .Include(p => p.MarketplaceLinks)
            .FirstAsync(p => p.Id == product.Id, cancellationToken);
    }

    public async Task<OptimizedPostPayloadDto> GenerateOptimizedPayloadForPostAsync(
        Guid globalProductId,
        MarketplaceType platform,
        CancellationToken cancellationToken = default)
    {
        var product = await _db.GlobalAdvertisingProducts
            .Include(p => p.MarketplaceLinks)
            .FirstOrDefaultAsync(p => p.GlobalProductUid == globalProductId, cancellationToken)
            ?? throw new InvalidOperationException($"Produto global {globalProductId} não encontrado.");

        var link = product.MarketplaceLinks.FirstOrDefault(l => l.MarketplaceType == platform)
            ?? throw new InvalidOperationException(
                $"Nenhum link de afiliado {platform} configurado para o produto {product.FriendlyName}.");

        return new OptimizedPostPayloadDto
        {
            GlobalProductId = product.GlobalProductUid,
            Platform = platform,
            FriendlyName = product.FriendlyName,
            FormattedPrice = product.AveragePrice.ToString("C2", PtBr),
            MainImageUrl = product.MainImageUrl,
            AffiliateLink = ShortenDisplayLink(link.GeneratedAffiliateLink),
            Description = TruncateDescription(product.Description),
            GlobalCategory = product.GlobalCategory
        };
    }

    public async Task<IReadOnlyList<GlobalAdvertisingProduct>> GetByOwnerAsync(
        int ownerId,
        CancellationToken cancellationToken = default)
    {
        var list = await _db.GlobalAdvertisingProducts
            .AsNoTracking()
            .Include(p => p.MarketplaceLinks)
            .Where(p => p.OwnerId == ownerId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        return list;
    }

    private Task<string> BuildAffiliateLinkAsync(
        MarketplaceType marketplace,
        string originalUrl,
        string? platformProductId,
        string trackingJson,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var trackingCode = ExtractTrackingCode(trackingJson);
        var separator = originalUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?';

        var link = marketplace switch
        {
            MarketplaceType.Shopee =>
                $"{originalUrl}{separator}utm_source=tecflow&sub_id={trackingCode}&mmp_pid={platformProductId ?? "shopee"}",
            MarketplaceType.TikTokShop =>
                $"{originalUrl}{separator}affiliate_id=tecflow&campaign_id={trackingCode}&product_id={platformProductId ?? ""}",
            _ => originalUrl
        };

        return Task.FromResult(link.Trim());
    }

    private static string NormalizeTrackingParameters(string? custom, Guid productUid)
    {
        if (!string.IsNullOrWhiteSpace(custom))
        {
            return custom.Trim();
        }

        return JsonSerializer.Serialize(new
        {
            sub_id = productUid.ToString("N")[..12],
            source = "tecflow",
            created = DateTime.UtcNow.ToString("o")
        });
    }

    private static string ExtractTrackingCode(string trackingJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(trackingJson);
            if (doc.RootElement.TryGetProperty("sub_id", out var sub))
            {
                return sub.GetString() ?? Guid.NewGuid().ToString("N")[..8];
            }
        }
        catch
        {
            // plain text tracking
        }

        return trackingJson.Length <= 32 ? trackingJson : trackingJson[..32];
    }

    private static string ShortenDisplayLink(string url) =>
        url.Length <= 120 ? url : url[..117] + "...";

    private static string? TruncateDescription(string? description) =>
        string.IsNullOrWhiteSpace(description)
            ? null
            : description.Length <= 280
                ? description
                : description[..277] + "...";
}
