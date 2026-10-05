using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Infrastructure.Services.Groups;

namespace TecFlow.Tests.Unit.Groups;

public class GroupCapturedMessagesServiceTests
{
    [Fact]
    public void ApplyRelevanceFilter_ShouldKeepConnectedStoreAndHideIgnored()
    {
        var service = new GroupCapturedMessagesService(null!);
        var items = new List<GroupCapturedMessage>
        {
            new() { Id = 1, HasDirectProductUrl = true, IsIgnored = false, PlatformType = MarketplaceType.Shopee },
            new() { Id = 2, HasDirectProductUrl = true, IsIgnored = false, PlatformType = MarketplaceType.Amazon },
            new() { Id = 3, HasDirectProductUrl = false, IsIgnored = false, PlatformType = MarketplaceType.Shopee },
            new() { Id = 4, HasDirectProductUrl = true, IsIgnored = true, PlatformType = MarketplaceType.Shopee }
        }.AsQueryable();

        var feed = service.ApplyRelevanceFilter(items, [MarketplaceType.Shopee], ignored: false).ToList();
        var hidden = service.ApplyRelevanceFilter(items, [MarketplaceType.Shopee], ignored: true).ToList();

        Assert.Equal(1, Assert.Single(feed).Id);
        Assert.Equal(4, Assert.Single(hidden).Id);
    }
}
