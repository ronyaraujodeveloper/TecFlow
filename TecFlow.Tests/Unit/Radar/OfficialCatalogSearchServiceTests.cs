using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Database;
using TecFlow.Database.Filter;
using TecFlow.Infrastructure.Services.Radar;
using TecFlow.Util.Security;

namespace TecFlow.Tests.Unit.Radar;

public class OfficialCatalogSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_ShouldExposeChannelStatusAndKeepPublicMlResults()
    {
        var ml = new Mock<IMercadoLivreApiService>();
        ml.Setup(x => x.SearchProductsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
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
    public async Task SearchAsync_ShouldSurfaceMercadoLivreHttpErrorOnChannelBadge()
    {
        var ml = new Mock<IMercadoLivreApiService>();
        ml.Setup(x => x.SearchProductsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
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
        Assert.Equal("Requer conta conectada no painel", mlStatus.Message);
    }

    private static AppDbContext CreateDbContext()
    {
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns<string>(s => s);
        encryption.Setup(e => e.Decrypt(It.IsAny<string>())).Returns<string>(s => s);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options, encryption.Object, new TecFlow.Database.MultiTenancy.NullCurrentTenantService());
    }
}
