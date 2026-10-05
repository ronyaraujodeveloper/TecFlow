using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Groups;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Business.Service.PublicPages;

namespace TecFlow.Infrastructure.Services.LinkStrategies;

public sealed class AffiliateLinkConverterService : IAffiliateLinkConverterService
{
    private readonly IUrlResolverService _urlResolver;
    private readonly IAffiliateLinkGenerationService _generation;
    private readonly IMarketplaceAccountRepository _marketplaceAccounts;
    private readonly IProductMetadataService _metadata;

    public AffiliateLinkConverterService(
        IUrlResolverService urlResolver,
        IAffiliateLinkGenerationService generation,
        IMarketplaceAccountRepository marketplaceAccounts,
        IProductMetadataService metadata)
    {
        _urlResolver = urlResolver;
        _generation = generation;
        _marketplaceAccounts = marketplaceAccounts;
        _metadata = metadata;
    }

    public async Task<AffiliateLinkConverterResponseDto> ConvertAsync(
        int userId,
        string capturedUrl,
        string? sourceGroup,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(capturedUrl))
        {
            return Fail(CloneOfferRules.ResolveFailedMessage);
        }

        var resolved = await _urlResolver.ResolveCanonicalAsync(capturedUrl, cancellationToken);
        var canonical = string.IsNullOrWhiteSpace(resolved.CanonicalUrl)
            ? capturedUrl.Trim()
            : resolved.CanonicalUrl;
        if (!UrlUnshortenerService.TryDetectMarketplace(canonical, out var platform)
            && !UniversalLinkResolverEngine.TryMapDomainToPlatform(canonical, out platform))
        {
            return Fail(CloneOfferRules.ResolveFailedMessage);
        }

        var accounts = await _marketplaceAccounts.ListByUserIdAsync(userId.ToString(), cancellationToken);
        var account = PublicConverterRules.FirstActiveForPlatform(accounts, platform);
        if (account is null)
        {
            return Fail(CloneOfferRules.MissingStoreMessage);
        }

        var request = new GerarLinkAfiliadoDto
        {
            OriginalUrl = capturedUrl.Trim(),
            StoreId = IntegracaoLojaScopeHelper.EncodeStoreScope(account.Id),
            StoreIds = [IntegracaoLojaScopeHelper.EncodeStoreScope(account.Id)],
            TenantId = account.TenantId,
            ShopId = account.ShopId,
            Source = CloneOfferRules.GroupCloneSource,
            SourceGroup = sourceGroup,
            CustomNickname = string.IsNullOrWhiteSpace(sourceGroup) ? null : sourceGroup.Trim()
        };

        var generated = await _generation.GenerateAsync(request, userId, cancellationToken);
        var affiliateUrl = FirstNonEmpty(generated.AffiliateUrl, generated.ResolvedShortUrl, generated.ConvertedUrl);
        if ((!generated.Success && !generated.HasConvertedLink) || string.IsNullOrWhiteSpace(affiliateUrl))
        {
            return Fail(string.IsNullOrWhiteSpace(generated.Message)
                ? CloneOfferRules.MissingStoreMessage
                : generated.Message);
        }

        var metadata = generated.ProductName is null && generated.ProductPrice is null
            ? await _metadata.ExtractAsync(canonical, cancellationToken)
            : null;

        return new AffiliateLinkConverterResponseDto
        {
            Status = true,
            Descricao = CloneOfferRules.SuccessToast,
            Data = new AffiliateLinkConverterResultDto
            {
                OriginalUrl = capturedUrl.Trim(),
                CanonicalUrl = canonical,
                AffiliateUrl = affiliateUrl,
                Title = FirstNonEmpty(generated.ProductName, metadata?.ProductName),
                Price = generated.ProductPrice ?? metadata?.ProductPrice,
                ImageUrl = FirstNonEmpty(generated.ProductImageUrl, metadata?.ProductImageUrl),
                Platform = platform,
                SourceGroup = string.IsNullOrWhiteSpace(sourceGroup) ? null : sourceGroup.Trim()
            }
        };
    }

    private static AffiliateLinkConverterResponseDto Fail(string message) =>
        new() { Status = false, Descricao = message };

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
