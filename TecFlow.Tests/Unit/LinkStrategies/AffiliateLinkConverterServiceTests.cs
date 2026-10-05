using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Groups;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Infrastructure.Services.LinkStrategies;

namespace TecFlow.Tests.Unit.LinkStrategies;

public class AffiliateLinkConverterServiceTests
{
    [Fact]
    public async Task ConvertAsync_ShouldPersistCommissionLinkFromResolvedStoreUrl()
    {
        var converter = new AffiliateLinkConverterService(
            new StubResolver("https://www.mercadolivre.com.br/p/MLB1"),
            new StubGeneration(),
            new StubAccounts(),
            new StubMetadata());

        var result = await converter.ConvertAsync(7, "https://meli.la/abc", "Pelando Promoções");

        Assert.True(result.Status);
        Assert.Equal(CloneOfferRules.SuccessToast, result.Descricao);
        Assert.Equal("https://mercadolivre.com.br/afiliado/x", result.Data?.AffiliateUrl);
        Assert.Equal("Cadeira gamer", result.Data?.Title);
        Assert.Equal(199.9m, result.Data?.Price);
        Assert.Equal("Pelando Promoções", result.Data?.SourceGroup);
    }

    private sealed class StubResolver : IUrlResolverService
    {
        private readonly string _canonical;

        public StubResolver(string canonical) => _canonical = canonical;

        public Task<UrlResolverResultDto> ResolveCanonicalAsync(
            string capturedUrl,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new UrlResolverResultDto
            {
                CanonicalUrl = _canonical,
                IsMarketplace = true,
                Platform = MarketplaceType.MercadoLivre
            });
    }

    private sealed class StubGeneration : IAffiliateLinkGenerationService
    {
        public Task<GerarLinkAfiliadoResponseDto> GenerateAsync(
            GerarLinkAfiliadoDto request,
            int userId,
            CancellationToken cancellationToken = default)
        {
            Assert.Equal(CloneOfferRules.GroupCloneSource, request.Source);
            Assert.Equal("Pelando Promoções", request.SourceGroup);
            return Task.FromResult(new GerarLinkAfiliadoResponseDto
            {
                Success = true,
                AffiliateUrl = "https://mercadolivre.com.br/afiliado/x",
                ConvertedUrl = "https://mercadolivre.com.br/afiliado/x",
                ProductName = "Cadeira gamer",
                ProductPrice = 199.9m,
                ProductImageUrl = "https://cdn.example.com/cadeira.jpg"
            });
        }
    }

    private sealed class StubAccounts : IMarketplaceAccountRepository
    {
        public Task<MarketplaceAccount?> GetByShopAsync(string shopId, MarketplaceType marketplaceType) =>
            Task.FromResult<MarketplaceAccount?>(null);

        public Task<IReadOnlyList<MarketplaceAccount>> ListForCurrentTenantAsync(bool consolidatedAllShops = true) =>
            Task.FromResult<IReadOnlyList<MarketplaceAccount>>([]);

        public Task<IReadOnlyList<MarketplaceAccount>> ListForShopAsync(string shopId) =>
            Task.FromResult<IReadOnlyList<MarketplaceAccount>>([]);

        public Task<IReadOnlyList<MarketplaceAccount>> ListByUserIdAsync(
            string userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MarketplaceAccount>>(
            [
                new MarketplaceAccount
                {
                    Id = 3,
                    UserId = userId,
                    MarketplaceType = MarketplaceType.MercadoLivre,
                    IsActive = true,
                    ShopId = "ml-1",
                    TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111")
                }
            ]);

        public Task SanitizeHttpTrackingIdsAsync(string userId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> ExistsActiveTrackingIdAsync(
            MarketplaceType marketplaceType,
            string trackingId,
            int? excludeAccountId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<MarketplaceAccount?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<MarketplaceAccount?>(null);

        public Task<bool> SetInactiveByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task UpsertAsync(MarketplaceAccount account) => Task.CompletedTask;
    }

    private sealed class StubMetadata : IProductMetadataService
    {
        public Task<ProductMetadataDto> ExtractAsync(string productUrl, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ProductMetadataDto());
    }
}
