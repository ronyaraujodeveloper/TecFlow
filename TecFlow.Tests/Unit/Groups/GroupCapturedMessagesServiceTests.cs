using TecFlow.Business.Service.Groups;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Infrastructure.Services.Groups;

namespace TecFlow.Tests.Unit.Groups;

public class GroupCapturedMessagesServiceTests
{
    [Fact]
    public void ApplyRelevanceFilter_ShouldKeepConnectedStoreAndHideIgnored()
    {
        var service = new GroupCapturedMessagesService(null!, new StructuredOfferParserService(), null!);
        var items = new List<GroupCapturedMessage>
        {
            new() { Id = 1, HasDirectProductUrl = true, IsIgnored = false, IsAvailable = true, PlatformType = MarketplaceType.Shopee },
            new() { Id = 2, HasDirectProductUrl = true, IsIgnored = false, IsAvailable = true, PlatformType = MarketplaceType.Amazon },
            new() { Id = 3, HasDirectProductUrl = false, IsIgnored = false, IsAvailable = true, PlatformType = MarketplaceType.Shopee },
            new() { Id = 4, HasDirectProductUrl = true, IsIgnored = true, IsAvailable = true, PlatformType = MarketplaceType.Shopee },
            new() { Id = 5, HasDirectProductUrl = true, IsIgnored = false, IsAvailable = false, PlatformType = MarketplaceType.Shopee }
        }.AsQueryable();

        var feed = service.ApplyRelevanceFilter(items, [MarketplaceType.Shopee], ignored: false).ToList();
        var hidden = service.ApplyRelevanceFilter(items, [MarketplaceType.Shopee], ignored: true).ToList();

        Assert.Equal(1, Assert.Single(feed).Id);
        Assert.Equal(4, Assert.Single(hidden).Id);
    }

    [Fact]
    public void ApplyStructuredParse_ShouldFillCouponAndSanitizeImagePath()
    {
        var service = new GroupCapturedMessagesService(null!, new StructuredOfferParserService(), null!);
        var entity = new GroupCapturedMessage
        {
            RawText = "🔥 Fone X\nCUPOM: SURPRESAMELIMAIS\nhttps://shopee.com.br/produto-i.1.2",
            ProductImageUrl = @"C:\inetpub\tecflow\api\wwwroot\uploads\products\1\2026\10\foto.jpg"
        };

        service.ApplyStructuredParse(entity, entity.RawText);

        Assert.Equal("Fone X", entity.ProductName);
        Assert.Equal("SURPRESAMELIMAIS", entity.CouponCode);
        Assert.Equal("/uploads/products/1/2026/10/foto.jpg", entity.ProductImageUrl);
    }
}
