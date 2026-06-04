using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TecFlow.Business.Dto;
using TecFlow.Core.Enums;
using TecFlow.Database;
using TecFlow.Infrastructure.Services.Advertising;
using TecFlow.Util.Security;

namespace TecFlow.Tests.Unit.Advertising;

public class AdvertisingProductServiceTests
{
    [Fact]
    public async Task CreateGlobalProductAsync_ShouldPersistProductAndMarketplaceLinks()
    {
        await using var db = CreateDbContext();
        var service = new AdvertisingProductService(db, NullLogger<AdvertisingProductService>.Instance);

        var created = await service.CreateGlobalProductAsync(1, new GlobalAdvertisingProductDto
        {
            FriendlyName = "Fone Bluetooth Pro",
            Description = "Som premium para divulgação.",
            GlobalCategory = "Tech",
            AveragePrice = 89.90m,
            MainImageUrl = "https://cdn.example/img.jpg",
            MarketplaceLinks =
            [
                new MarketplaceAffiliateLinkDto
                {
                    MarketplaceType = MarketplaceType.Shopee,
                    OriginalProductUrl = "https://shopee.com.br/item/1"
                },
                new MarketplaceAffiliateLinkDto
                {
                    MarketplaceType = MarketplaceType.TikTokShop,
                    OriginalProductUrl = "https://shop.tiktok.com/product/2"
                }
            ]
        });

        Assert.Equal("Fone Bluetooth Pro", created.FriendlyName);
        Assert.Equal(2, created.MarketplaceLinks.Count);
        Assert.All(created.MarketplaceLinks, l => Assert.False(string.IsNullOrWhiteSpace(l.GeneratedAffiliateLink)));
    }

    [Fact]
    public async Task GenerateOptimizedPayloadForPostAsync_ShouldReturnFormattedMetadata()
    {
        await using var db = CreateDbContext();
        var service = new AdvertisingProductService(db, NullLogger<AdvertisingProductService>.Instance);

        var created = await service.CreateGlobalProductAsync(1, new GlobalAdvertisingProductDto
        {
            FriendlyName = "Kit Skincare",
            GlobalCategory = "Beleza",
            AveragePrice = 120m,
            MarketplaceLinks =
            [
                new MarketplaceAffiliateLinkDto
                {
                    MarketplaceType = MarketplaceType.Shopee,
                    OriginalProductUrl = "https://shopee.com.br/x"
                }
            ]
        });

        var payload = await service.GenerateOptimizedPayloadForPostAsync(
            created.GlobalProductUid,
            MarketplaceType.Shopee);

        Assert.Equal("Kit Skincare", payload.FriendlyName);
        Assert.Equal(MarketplaceType.Shopee, payload.Platform);
        Assert.Contains("tecflow", payload.AffiliateLink, StringComparison.OrdinalIgnoreCase);
    }

    private static AppDbContext CreateDbContext()
    {
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns<string>(s => s);
        encryption.Setup(e => e.Decrypt(It.IsAny<string>())).Returns<string>(s => s);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options, encryption.Object);
    }
}
