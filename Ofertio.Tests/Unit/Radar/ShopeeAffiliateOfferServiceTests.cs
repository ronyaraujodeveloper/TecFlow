using Ofertio.Core.Enums;
using Ofertio.Infrastructure.Services.Radar;

namespace Ofertio.Tests.Unit.Radar;

public class ShopeeAffiliateOfferServiceTests
{
    [Fact]
    public void ParseSearch_ShouldMapCommissionSalesRatingDiscountAndShopType()
    {
        const string json = """
            {"data":{"productOfferV2":{"nodes":[
              {"itemId":111,"shopId":222,"productName":"Fone Bluetooth","priceMin":"80.00","productLink":"https://shopee.com.br/p/111","shopName":"Loja Oficial","shopType":[1],"commissionRate":"0.08","commission":"6.40","sales":1530,"ratingStar":"4.83","priceDiscountRate":20},
              {"itemId":333,"shopId":444,"productName":"Cabo USB","priceMin":10,"productLink":"https://shopee.com.br/p/333","shopType":[2],"commissionRate":"0","sales":0}
            ]}}}
            """;

        var items = ShopeeAffiliateOfferService.ParseSearch(json);

        Assert.Equal(2, items.Count);
        var fone = items[0];
        Assert.Equal(MarketplaceType.Shopee, fone.PlatformType);
        Assert.Equal(80m, fone.Price);
        Assert.Equal(100m, fone.OriginalPrice);
        Assert.Equal(8m, fone.CommissionPercent);
        Assert.Equal(6.40m, fone.EstimatedCommission);
        Assert.Equal("API Shopee", fone.CommissionSource);
        Assert.Equal(1530, fone.SoldQuantity);
        Assert.Equal(4.8m, fone.RatingAverage);
        Assert.Equal("Loja Oficial", fone.SellerName);
        Assert.True(fone.IsOfficialStore);

        var cabo = items[1];
        Assert.Null(cabo.CommissionPercent);
        Assert.Null(cabo.SoldQuantity);
        Assert.False(cabo.IsOfficialStore);
        Assert.Equal("preferred", cabo.SellerPowerStatus);
    }
}
