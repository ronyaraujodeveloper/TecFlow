using Ofertio.Business.Mappings;
using Ofertio.Core.Entities;
using Ofertio.Core.Enums;
using Ofertio.Database.Entity;

namespace Ofertio.Tests.Unit.Mappings;

public class MarketplaceAccountMapperTests
{
    [Fact]
    public void ToDto_KeepsTheAccountIdAndExposesTheIntegrationId()
    {
        var dto = MarketplaceAccountMapper.ToDto(
            new MarketplaceAccount
            {
                Id = 17,
                UserId = "1",
                ShopId = "ul-tt-1-achadinhocerto",
                MarketplaceType = MarketplaceType.TikTokShop,
                IsActive = true
            },
            new IntegracaoLoja
            {
                Id = 22,
                UserId = 1,
                ShopId = "ul-tt-1-achadinhocerto",
                PlatformType = MarketplaceType.TikTokShop
            });

        Assert.Equal(17, dto.Id);
        Assert.Equal(22, dto.IntegracaoLojaId);
    }
}
