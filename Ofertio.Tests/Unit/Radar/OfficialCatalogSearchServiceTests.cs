using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Core.Enums;
using Ofertio.Database;
using Ofertio.Database.Filter;
using Ofertio.Infrastructure.Services.Radar;
using Ofertio.Util.Security;

namespace Ofertio.Tests.Unit.Radar;

public class OfficialCatalogSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_ShouldExposeChannelStatusAndKeepPublicMlResults()
    {
        var ml = new Mock<IMercadoLivreApiService>();
        ml.Setup(x => x.SearchCatalogAsync(It.IsAny<int>(), It.IsAny<MercadoLivreCatalogSearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OfficialCatalogChannelResult.From(
            [
                new OfficialCatalogProductDto
                {
                    Platform = "MercadoLivre",
                    ProductName = "Notebook",
                    SourceUrl = "https://produto.mercadolivre.com.br/MLB-1"
                }
            ]));
        var shopee = new Mock<IShopeeAffiliateOfferService>();
        shopee.Setup(x => x.SearchProductsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OfficialCatalogChannelResult.Unconfigured());
        var amazon = new Mock<IAmazonPaApiService>();
        amazon.Setup(x => x.SearchProductsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OfficialCatalogChannelResult.Unconfigured());

        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        factory.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDbContext);

        var sut = new OfficialCatalogSearchService(
            ml.Object,
            shopee.Object,
            amazon.Object,
            factory.Object,
            NullLogger<OfficialCatalogSearchService>.Instance);

        var result = await sut.SearchAsync(1, new OfficialCatalogSearchFilter { Keyword = "notebook" });

        Assert.True(result.Status);
        Assert.Single(result.DataList!);
        Assert.Equal("ok", result.Channels.Single(c => c.Key == "MercadoLivre").State);
        Assert.Equal("Chave de API não configurada", result.Channels.Single(c => c.Key == "Shopee").Message);
        Assert.Equal("PA-API não configurada", result.Channels.Single(c => c.Key == "Amazon").Message);
        Assert.Equal("0 produtos", result.Channels.Single(c => c.Key == "Local").Message);
    }

    [Fact]
    public async Task SearchAsync_ShouldKeepMercadoLivreCardsWhenTitleOmitsKeyword()
    {
        var ml = new Mock<IMercadoLivreApiService>();
        ml.Setup(x => x.SearchCatalogAsync(It.IsAny<int>(), It.IsAny<MercadoLivreCatalogSearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OfficialCatalogChannelResult.From(
            [
                new OfficialCatalogProductDto
                {
                    Platform = "Mercado Livre",
                    PlatformType = MarketplaceType.MercadoLivre,
                    ProductName = "Ultrabook Gamer",
                    SourceUrl = "https://www.mercadolivre.com.br/ultrabook/p/MLB-1"
                },
                new OfficialCatalogProductDto
                {
                    Platform = "Mercado Livre",
                    PlatformType = MarketplaceType.MercadoLivre,
                    ProductName = "Mini PC",
                    SourceUrl = "https://www.mercadolivre.com.br/mini-pc/p/MLB-2"
                }
            ]));
        var shopee = new Mock<IShopeeAffiliateOfferService>();
        shopee.Setup(x => x.SearchProductsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OfficialCatalogChannelResult.Empty);
        var amazon = new Mock<IAmazonPaApiService>();
        amazon.Setup(x => x.SearchProductsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OfficialCatalogChannelResult.Empty);
        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        factory.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDbContext);

        var sut = new OfficialCatalogSearchService(
            ml.Object,
            shopee.Object,
            amazon.Object,
            factory.Object,
            NullLogger<OfficialCatalogSearchService>.Instance);

        var result = await sut.SearchAsync(1, new OfficialCatalogSearchFilter { Keyword = "notebook 16GB" });

        Assert.Equal(2, result.DataList!.Count);
        Assert.Equal("ok", result.Channels.Single(c => c.Key == "MercadoLivre").State);
    }

    [Fact]
    public async Task SearchAsync_ShouldSurfaceMercadoLivreHttpErrorOnChannelBadge()
    {
        var ml = new Mock<IMercadoLivreApiService>();
        ml.Setup(x => x.SearchCatalogAsync(It.IsAny<int>(), It.IsAny<MercadoLivreCatalogSearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OfficialCatalogChannelResult.Unconfigured());
        var shopee = new Mock<IShopeeAffiliateOfferService>();
        shopee.Setup(x => x.SearchProductsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OfficialCatalogChannelResult.Empty);
        var amazon = new Mock<IAmazonPaApiService>();
        amazon.Setup(x => x.SearchProductsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OfficialCatalogChannelResult.Empty);
        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        factory.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDbContext);

        var sut = new OfficialCatalogSearchService(
            ml.Object,
            shopee.Object,
            amazon.Object,
            factory.Object,
            NullLogger<OfficialCatalogSearchService>.Instance);

        var result = await sut.SearchAsync(1, new OfficialCatalogSearchFilter { Keyword = "dell i7" });
        var mlStatus = result.Channels.Single(c => c.Key == "MercadoLivre");
        Assert.Equal("missing", mlStatus.State);
        Assert.Equal("Requer OAuth do Mercado Livre para buscar ofertas", mlStatus.Message);
    }

    [Fact]
    public async Task SearchAsync_ShouldPassScanFiltersToMercadoLivreAndExposeScanResult()
    {
        MercadoLivreCatalogSearchRequest? captured = null;
        var ml = new Mock<IMercadoLivreApiService>();
        ml.Setup(x => x.SearchCatalogAsync(It.IsAny<int>(), It.IsAny<MercadoLivreCatalogSearchRequest>(), It.IsAny<CancellationToken>()))
            .Callback<int, MercadoLivreCatalogSearchRequest, CancellationToken>((_, request, _) => captured = request)
            .ReturnsAsync(new OfficialCatalogChannelResult
            {
                Items =
                [
                    MlItem("A", 300m, 310m, "Frete grátis"),
                    MlItem("B", 100m, 200m, "Frete grátis"),
                    MlItem("C", 200m, null, "Frete grátis")
                ],
                ScannedCount = 60,
                NextOffset = 60,
                EffectiveQuery = "dell inspiron",
                Categories = [new OfficialCatalogCategoryDto { Id = "MLB-NOTEBOOKS", Name = "Notebooks" }]
            });
        var sut = CreateSut(ml);

        var result = await sut.SearchAsync(1, new OfficialCatalogSearchFilter
        {
            Keyword = "dell inspiron i7",
            MinPrice = 50m,
            MaxPrice = 500m,
            FreeShippingOnly = true,
            SortBy = "menor_preco",
            MercadoLivreCategory = "MLB-NOTEBOOKS",
            MercadoLivre = true
        });

        Assert.NotNull(captured);
        Assert.Equal("dell inspiron i7", captured!.Query);
        Assert.Equal(50m, captured.MinPrice);
        Assert.Equal(500m, captured.MaxPrice);
        Assert.True(captured.FreeShippingOnly);
        Assert.Equal("MLB-NOTEBOOKS", captured.DomainId);
        Assert.Equal(0, captured.Offset);
        Assert.True(captured.AllowShorterQuery);
        Assert.True(captured.IncludeCategories);
        Assert.Equal(["B", "C", "A"], result.DataList!.Select(item => item.ProductId));
        Assert.Equal(60, result.MercadoLivreNextOffset);
        Assert.Equal("dell inspiron", result.MercadoLivreEffectiveQuery);
        Assert.Equal("MLB-NOTEBOOKS", Assert.Single(result.MercadoLivreCategories).Id);
        var mlStatus = result.Channels.Single(c => c.Key == "MercadoLivre");
        Assert.Equal("ok", mlStatus.State);
        Assert.Equal("3 ofertas · 60 produtos analisados · buscado como \"dell inspiron\"", mlStatus.Message);
    }

    [Fact]
    public async Task SearchAsync_LoadMore_ShouldSendOffsetWithoutShorterQueryAndSkipLocalBase()
    {
        MercadoLivreCatalogSearchRequest? captured = null;
        var ml = new Mock<IMercadoLivreApiService>();
        ml.Setup(x => x.SearchCatalogAsync(It.IsAny<int>(), It.IsAny<MercadoLivreCatalogSearchRequest>(), It.IsAny<CancellationToken>()))
            .Callback<int, MercadoLivreCatalogSearchRequest, CancellationToken>((_, request, _) => captured = request)
            .ReturnsAsync(new OfficialCatalogChannelResult { ScannedCount = 40 });
        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        var sut = CreateSut(ml, factory);

        var result = await sut.SearchAsync(1, new OfficialCatalogSearchFilter
        {
            Keyword = "dell",
            MercadoLivre = true,
            MercadoLivreOffset = 40
        });

        Assert.Equal(40, captured!.Offset);
        Assert.False(captured.AllowShorterQuery);
        Assert.False(captured.IncludeCategories);
        factory.Verify(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()), Times.Never);
        var mlStatus = result.Channels.Single(c => c.Key == "MercadoLivre");
        Assert.Equal("0 ofertas · 40 produtos analisados (sem anúncio ativo)", mlStatus.Message);
        Assert.Null(result.MercadoLivreNextOffset);
    }

    private static OfficialCatalogProductDto MlItem(string id, decimal price, decimal? original, string shipping) => new()
    {
        Platform = "Mercado Livre",
        PlatformType = MarketplaceType.MercadoLivre,
        ProductId = id,
        ProductName = id,
        Price = price,
        OriginalPrice = original,
        Shipping = shipping,
        SourceUrl = $"https://www.mercadolivre.com.br/p/{id}"
    };

    private static OfficialCatalogSearchService CreateSut(
        Mock<IMercadoLivreApiService> ml,
        Mock<IDbContextFactory<AppDbContext>>? factory = null)
    {
        var shopee = new Mock<IShopeeAffiliateOfferService>();
        shopee.Setup(x => x.SearchProductsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OfficialCatalogChannelResult.Empty);
        var amazon = new Mock<IAmazonPaApiService>();
        amazon.Setup(x => x.SearchProductsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OfficialCatalogChannelResult.Empty);
        if (factory is null)
        {
            factory = new Mock<IDbContextFactory<AppDbContext>>();
            factory.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateDbContext);
        }

        return new OfficialCatalogSearchService(
            ml.Object,
            shopee.Object,
            amazon.Object,
            factory.Object,
            NullLogger<OfficialCatalogSearchService>.Instance);
    }

    private static AppDbContext CreateDbContext()
    {
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns<string>(s => s);
        encryption.Setup(e => e.Decrypt(It.IsAny<string>())).Returns<string>(s => s);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options, encryption.Object, new Ofertio.Database.MultiTenancy.NullCurrentTenantService());
    }
}
