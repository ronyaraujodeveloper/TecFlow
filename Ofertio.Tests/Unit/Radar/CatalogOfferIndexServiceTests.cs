using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Ofertio.Business.Dto;
using Ofertio.Business.Integrations.MercadoLivre;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Radar;
using Ofertio.Core.Entities;
using Ofertio.Core.Enums;
using Ofertio.Database;
using Ofertio.Database.Filter;
using Ofertio.Infrastructure.Services.Radar;
using Ofertio.Util.Security;

namespace Ofertio.Tests.Unit.Radar;

public class CatalogOfferIndexServiceTests
{
    [Fact]
    public async Task UpsertAsync_ShouldInsertThenUpdateTheSameUrlAndExtractTheModel()
    {
        await using var context = RadarHybridTestDb.CreateDbContext();
        var sut = new CatalogOfferIndexService(context);
        var offer = new OfficialCatalogProductDto
        {
            Platform = "Mercado Livre",
            PlatformType = MarketplaceType.MercadoLivre,
            ProductName = "Smart TV Philips 43 43PFG6825/78",
            Price = 1899m,
            Shipping = "Frete grátis",
            CommissionPercent = 12.5m,
            SourceUrl = "https://www.mercadolivre.com.br/p/MLB1"
        };

        await sut.UpsertAsync(4, "Philips", [offer]);
        offer.Price = 1599m;
        await sut.UpsertAsync(4, "philips", [offer]);

        var rows = await context.IndexedCatalogOffers.ToListAsync();
        var row = Assert.Single(rows);
        Assert.Equal("philips", row.SearchTerm);
        Assert.Equal("Philips 43PFG6825", row.ProductModel);
        Assert.Equal(1599m, row.Price);
        Assert.Equal(12.5m, row.CommissionPercent);
        Assert.True(await sut.HasFreshTermAsync(4, "Philips", DateTime.UtcNow.AddMinutes(-1)));
        Assert.False(await sut.HasFreshTermAsync(4, "casa", DateTime.UtcNow.AddMinutes(-1)));
    }
}

public class OfferRadarHybridQueryTests
{
    [Fact]
    public async Task Refresh_ShouldSkipOfficialApisWhenTheTermIsFresh()
    {
        await using var context = RadarHybridTestDb.CreateDbContext();
        context.IndexedCatalogOffers.Add(SampleOffer(indexedAt: DateTime.UtcNow));
        await context.SaveChangesAsync();
        var catalog = new Mock<IOfficialCatalogSearchService>();
        var sut = CreateHybrid(context, catalog.Object, cacheMinutes: "15");

        var note = await sut.RefreshProfileTermsAsync(4, Profile(), OfferIndexRules.InteractiveRefreshTermCap);

        Assert.Null(note);
        catalog.Verify(x => x.SearchAsync(It.IsAny<int>(), It.IsAny<OfficialCatalogSearchFilter>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(OfferRadarSources.Catalog, Assert.Single(context.OfferRadarItems).Source);
    }

    [Fact]
    public async Task Refresh_ShouldCallOfficialApisWhenLocalResultsAreOlderThanTheCache()
    {
        await using var context = RadarHybridTestDb.CreateDbContext();
        context.IndexedCatalogOffers.Add(SampleOffer(indexedAt: DateTime.UtcNow.AddMinutes(-30)));
        await context.SaveChangesAsync();
        var catalog = new Mock<IOfficialCatalogSearchService>();
        catalog.Setup(x => x.SearchAsync(4, It.IsAny<OfficialCatalogSearchFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OfficialCatalogSearchResponseDto { Status = true, DataList = [] });
        var sut = CreateHybrid(context, catalog.Object, cacheMinutes: "15");

        var note = await sut.RefreshProfileTermsAsync(4, Profile(), OfferIndexRules.WorkerRefreshTermCap);

        Assert.Contains("1 termo", note);
        catalog.Verify(x => x.SearchAsync(
            4,
            It.Is<OfficialCatalogSearchFilter>(filter => filter.Keyword == "games" && filter.Limit == OfferIndexRules.MiningSearchLimit),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Refresh_ShouldIgnoreFreshRowsWhenMasterCacheIsDisabled()
    {
        await using var context = RadarHybridTestDb.CreateDbContext();
        context.IndexedCatalogOffers.Add(SampleOffer(indexedAt: DateTime.UtcNow));
        await context.SaveChangesAsync();
        var catalog = new Mock<IOfficialCatalogSearchService>();
        catalog.Setup(x => x.SearchAsync(It.IsAny<int>(), It.IsAny<OfficialCatalogSearchFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OfficialCatalogSearchResponseDto { Status = true, DataList = [] });
        var sut = CreateHybrid(context, catalog.Object, cacheMinutes: "0");

        await sut.RefreshProfileTermsAsync(4, Profile(), 5);

        catalog.Verify(x => x.SearchAsync(4, It.IsAny<OfficialCatalogSearchFilter>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static OfferRadarHybridQuery CreateHybrid(AppDbContext context, IOfficialCatalogSearchService catalog, string cacheMinutes)
    {
        var options = new Mock<IOptionsMonitor<MercadoLivreIntegrationOptions>>();
        options.SetupGet(item => item.CurrentValue).Returns(new MercadoLivreIntegrationOptions { SearchCacheMinutes = cacheMinutes });
        return new OfferRadarHybridQuery(
            context,
            new CatalogOfferIndexService(context),
            catalog,
            options.Object,
            NullLogger<OfferRadarHybridQuery>.Instance);
    }

    private static AffiliateMiningProfileDto Profile() => new() { Niches = ["Games"] };

    private static IndexedCatalogOffer SampleOffer(DateTime indexedAt) => new()
    {
        UserId = 4,
        SearchTerm = "games",
        ProductName = "Controle Xbox",
        ProductModel = "XBX1234",
        SourceUrl = "https://loja/controle",
        PlatformType = MarketplaceType.Amazon,
        PlatformName = "Amazon",
        Price = 299m,
        IndexedAt = indexedAt
    };
}

public class StorePriceComparisonServiceTests
{
    [Fact]
    public async Task CompareAsync_ShouldSearchTheModelAndHighlightTheLowestStore()
    {
        await using var context = RadarHybridTestDb.CreateDbContext();
        context.IndexedCatalogOffers.Add(new IndexedCatalogOffer
        {
            UserId = 4,
            SearchTerm = "philips 43pfg6825",
            ProductName = "Smart TV Philips 43PFG6825",
            ProductModel = "Philips 43PFG6825",
            SourceUrl = "https://shopee/tv",
            PlatformType = MarketplaceType.Shopee,
            PlatformName = "Shopee",
            Price = 1900m,
            Shipping = "Frete R$ 19,90",
            CommissionPercent = 7m,
            IndexedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var catalog = new Mock<IOfficialCatalogSearchService>();
        catalog.Setup(x => x.SearchAsync(4, It.IsAny<OfficialCatalogSearchFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OfficialCatalogSearchResponseDto
            {
                Status = true,
                DataList =
                [
                    new OfficialCatalogProductDto
                    {
                        Platform = "Amazon",
                        PlatformType = MarketplaceType.Amazon,
                        ProductName = "TV Philips 43PFG6825",
                        Price = 1800m,
                        Shipping = "Frete grátis",
                        CommissionPercent = 4m,
                        SourceUrl = "https://amazon/tv"
                    },
                    new OfficialCatalogProductDto
                    {
                        Platform = "Mercado Livre",
                        PlatformType = MarketplaceType.MercadoLivre,
                        ProductName = "Smart TV Samsung UN55AU7700",
                        Price = 900m,
                        SourceUrl = "https://ml/samsung"
                    }
                ]
            });
        var sut = new StorePriceComparisonService(context, catalog.Object, NullLogger<StorePriceComparisonService>.Instance);

        var result = await sut.CompareAsync(4, new StorePriceComparisonRequestDto
        {
            ProductName = "Smart TV Philips 43 43PFG6825/78",
            SourceUrl = "https://ml/referencia",
            PlatformName = "Mercado Livre",
            PlatformType = MarketplaceType.MercadoLivre,
            Price = 2100m,
            Shipping = "Frete grátis"
        });

        Assert.True(result.Status);
        Assert.Equal("Philips 43PFG6825", result.Model);
        Assert.True(result.ExactModel);
        Assert.Equal(3, result.Rows.Count);
        Assert.DoesNotContain(result.Rows, row => row.SourceUrl.Contains("samsung", StringComparison.OrdinalIgnoreCase));
        var lowest = Assert.Single(result.Rows, row => row.IsLowestPrice);
        Assert.Equal(1800m, lowest.Price);
        Assert.Equal("Amazon", lowest.PlatformName);
        catalog.Verify(x => x.SearchAsync(
            4,
            It.Is<OfficialCatalogSearchFilter>(filter => filter.Keyword == "Philips 43PFG6825"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

file static class RadarHybridTestDb
{
    public static AppDbContext CreateDbContext()
    {
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(item => item.Encrypt(It.IsAny<string>())).Returns<string>(value => value);
        encryption.Setup(item => item.Decrypt(It.IsAny<string>())).Returns<string>(value => value);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options, encryption.Object, new Ofertio.Database.MultiTenancy.NullCurrentTenantService());
    }
}
