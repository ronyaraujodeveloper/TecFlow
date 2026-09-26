using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TecFlow.Business.Integrations.Auth;
using TecFlow.Business.Integrations.Shopee;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Core.Enums;
using TecFlow.Database.Entity;
using TecFlow.Infrastructure.Services.Service.ExternalServices;

namespace TecFlow.Tests.Unit.LinkStrategies;

public class ShopeeServiceTests
{
    [Fact]
    public async Task TryGetAffiliateItemDetailsAsync_ShouldReturnNull_WhenStoreHasNoOfficialCredentials()
    {
        var client = new Mock<IShopeeIntegrationClient>(MockBehavior.Strict);
        var accounts = new Mock<IMarketplaceAccountRepository>();
        accounts
            .Setup(repository => repository.GetByShopAsync("1890496775", MarketplaceType.Shopee))
            .ReturnsAsync((TecFlow.Core.Entities.MarketplaceAccount?)null);

        var service = new ShopeeService(
            client.Object,
            new Mock<IMarketplaceSignatureService>().Object,
            accounts.Object,
            Options.Create(new ShopeeIntegrationOptions
            {
                PartnerId = string.Empty,
                PartnerKey = string.Empty
            }),
            NullLogger<ShopeeService>.Instance);

        var result = await service.TryGetAffiliateItemDetailsAsync(
            "https://shopee.com.br/product/1890496775/23499652945",
            new IntegracaoLoja
            {
                Id = 9,
                ShopId = "1890496775",
                AccessToken = null
            });

        Assert.Null(result);
        client.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
