using Microsoft.Extensions.Logging.Abstractions;
using Ofertio.Business.Service.Groups;
using Ofertio.Core.Entities;
using Ofertio.Core.Enums;
using Ofertio.Infrastructure.Services.Groups;

namespace Ofertio.Tests.Unit.Groups;

public class GroupCapturedMessagesServiceTests
{
    [Fact]
    public void ApplyRelevanceFilter_ShouldKeepConnectedStoreAndHideIgnored()
    {
        var service = new GroupCapturedMessagesService(null!, new StructuredOfferParserService(), null!, null!, NullLogger<GroupCapturedMessagesService>.Instance);
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
    public void ApplyRelevanceFilter_ShouldKeepCouponsWithoutProductUrlOrConnectedStore()
    {
        var service = new GroupCapturedMessagesService(null!, new StructuredOfferParserService(), null!, null!, NullLogger<GroupCapturedMessagesService>.Instance);
        var items = new List<GroupCapturedMessage>
        {
            new() { Id = 1, Type = OfferType.Coupon, HasDirectProductUrl = false, IsAvailable = true, PlatformType = null },
            new() { Id = 2, Type = OfferType.Coupon, HasDirectProductUrl = true, IsAvailable = true, PlatformType = MarketplaceType.Amazon },
            new() { Id = 3, Type = OfferType.Coupon, HasDirectProductUrl = false, IsIgnored = true, IsAvailable = true },
            new() { Id = 4, Type = OfferType.Product, HasDirectProductUrl = true, IsAvailable = true, PlatformType = MarketplaceType.Amazon }
        }.AsQueryable();

        var feed = service.ApplyRelevanceFilter(items, [MarketplaceType.Shopee], ignored: false).Select(item => item.Id).ToList();
        var withoutStores = service.ApplyRelevanceFilter(items, [], ignored: false).Select(item => item.Id).ToList();

        Assert.Equal([1, 2], feed);
        Assert.Equal([1, 2], withoutStores);
    }

    [Fact]
    public void ApplyRelevanceFilter_ShouldHideProductsWithInactiveStatusEvenWhenStillFlaggedAvailable()
    {
        var service = new GroupCapturedMessagesService(null!, new StructuredOfferParserService(), null!, null!, NullLogger<GroupCapturedMessagesService>.Instance);
        var items = new List<GroupCapturedMessage>
        {
            new() { Id = 1, HasDirectProductUrl = true, IsAvailable = true, OfferStatus = GroupOfferStatuses.Ativo, PlatformType = MarketplaceType.MercadoLivre },
            new() { Id = 2, HasDirectProductUrl = true, IsAvailable = true, OfferStatus = GroupOfferStatuses.PrecoAlterado, PlatformType = MarketplaceType.MercadoLivre },
            new() { Id = 3, HasDirectProductUrl = true, IsAvailable = true, OfferStatus = GroupOfferStatuses.Esgotado, PlatformType = MarketplaceType.MercadoLivre },
            new() { Id = 4, HasDirectProductUrl = true, IsAvailable = true, OfferStatus = GroupOfferStatuses.Inativo, PlatformType = MarketplaceType.MercadoLivre },
            new() { Id = 5, HasDirectProductUrl = true, IsAvailable = false, OfferStatus = GroupOfferStatuses.Esgotado, PlatformType = MarketplaceType.MercadoLivre },
            new() { Id = 6, Type = OfferType.Coupon, IsAvailable = true, OfferStatus = GroupOfferStatuses.Esgotado }
        }.AsQueryable();

        var feed = service.ApplyRelevanceFilter(items, [MarketplaceType.MercadoLivre], ignored: false).Select(item => item.Id).ToList();

        Assert.Equal([1, 2, 6], feed);
    }

    [Fact]
    public void ApplyStructuredParse_ShouldFillCouponAndSanitizeImagePath()
    {
        var service = new GroupCapturedMessagesService(null!, new StructuredOfferParserService(), null!, null!, NullLogger<GroupCapturedMessagesService>.Instance);
        var entity = new GroupCapturedMessage
        {
            RawText = "🔥 Fone X\nCUPOM: SURPRESAMELIMAIS\nhttps://shopee.com.br/produto-i.1.2",
            ProductImageUrl = @"C:\inetpub\ofertio\api\wwwroot\uploads\products\1\2026\10\foto.jpg"
        };

        service.ApplyStructuredParse(entity, entity.RawText);

        Assert.Equal("Fone X", entity.ProductName);
        Assert.Equal("SURPRESAMELIMAIS", entity.CouponCode);
        Assert.Equal("/uploads/products/1/2026/10/foto.jpg", entity.ProductImageUrl);
    }
}
