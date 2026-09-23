using Microsoft.Extensions.Hosting;
using Moq;
using TecFlow.Business.Dto;
using TecFlow.Business.Integrations.Auth;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database.Entity;
using TecFlow.Database.Filter;
using TecFlow.Infrastructure.Services.Integrations;

namespace TecFlow.Tests.Unit.Integrations;

public class IntegracaoLojaServiceTests
{
    [Fact]
    public async Task LinkAsync_ShouldPersistStore_WhenHomologStubCodeAndNumericShopId()
    {
        var user = new UserAccount
        {
            Id = 7,
            Name = "Demo",
            Email = "demo@tecso.local",
            PasswordHash = "hash",
            TenantId = Guid.NewGuid()
        };

        var users = new Mock<IUserAccountRepository>();
        users.Setup(repository => repository.GetByIdAsync(7)).ReturnsAsync(user);

        var auth = new Mock<IMarketplaceAuthService>();
        auth.Setup(service => service.CallbackAndGenerateTokensAsync(
                MarketplaceType.Shopee,
                HomologMarketplaceAuth.StubAuthorizationCode,
                "ul-7-loja-homolog",
                It.IsAny<CancellationToken>(),
                It.IsAny<string?>()))
            .ReturnsAsync(new MarketplaceTokenResult
            {
                Success = true,
                Descricao = "OK",
                ShopId = "ul-7-loja-homolog",
                MarketplaceType = MarketplaceType.Shopee,
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            });

        var accounts = new Mock<IMarketplaceAccountRepository>();
        accounts.Setup(repository => repository.GetByShopAsync("ul-7-loja-homolog", MarketplaceType.Shopee))
            .ReturnsAsync((MarketplaceAccount?)null);
        MarketplaceAccount? savedAccount = null;
        accounts.Setup(repository => repository.UpsertAsync(It.IsAny<MarketplaceAccount>()))
            .Callback<MarketplaceAccount>(account => savedAccount = account)
            .Returns(Task.CompletedTask);

        IntegracaoLoja? saved = null;
        var stores = new Mock<IIntegracaoLojaRepository>();
        stores.Setup(repository => repository.GetByUserShopPlatformAsync(
                7, "ul-7-loja-homolog", MarketplaceType.Shopee, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IntegracaoLoja?)null);
        stores.Setup(repository => repository.AddAsync(It.IsAny<IntegracaoLoja>(), It.IsAny<CancellationToken>()))
            .Callback<IntegracaoLoja, CancellationToken>((entity, _) => saved = entity)
            .Returns(Task.CompletedTask);

        var service = new IntegracaoLojaService(stores.Object, users.Object, accounts.Object, auth.Object, Production());
        var result = await service.LinkAsync(7, new IntegracaoLojaDto
        {
            PlatformType = MarketplaceType.Shopee,
            TrackingId = "18325850271",
            FriendlyName = "Loja Homolog"
        });

        Assert.True(result.Status);
        Assert.Contains("Universal Link", result.Descricao, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(saved);
        Assert.Equal("ul-7-loja-homolog", saved!.ShopId);
        Assert.Equal("18325850271", saved.AffiliateTrackingId);
        Assert.Equal("Loja Homolog", saved.FriendlyName);
        Assert.NotNull(savedAccount);
        Assert.Equal("ul-7-loja-homolog", savedAccount!.ShopId);
        Assert.Equal("18325850271", savedAccount.AffiliateTrackingId);
        auth.Verify(
            service => service.CallbackAndGenerateTokensAsync(
                It.IsAny<MarketplaceType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task LinkAsync_ShouldPersistAlphanumericTrackingId_WhenShopeeUniversalLink()
    {
        var (service, stores, auth, accounts) = CreateShopeeLinkMocks(userId: 1, shopKey: "ul-1-loja-homolog");
        IntegracaoLoja? saved = null;
        MarketplaceAccount? savedAccount = null;
        stores.Setup(repository => repository.AddAsync(It.IsAny<IntegracaoLoja>(), It.IsAny<CancellationToken>()))
            .Callback<IntegracaoLoja, CancellationToken>((entity, _) => saved = entity)
            .Returns(Task.CompletedTask);
        accounts.Setup(repository => repository.UpsertAsync(It.IsAny<MarketplaceAccount>()))
            .Callback<MarketplaceAccount>(account => savedAccount = account)
            .Returns(Task.CompletedTask);

        var result = await service.LinkAsync(1, new IntegracaoLojaDto
        {
            PlatformType = MarketplaceType.Shopee,
            TrackingId = "loja-abc",
            FriendlyName = "Loja Homolog"
        });

        Assert.True(result.Status);
        Assert.Equal("ul-1-loja-homolog", saved!.ShopId);
        Assert.Equal("loja-abc", saved.AffiliateTrackingId);
        Assert.Equal("loja-abc", savedAccount!.AffiliateTrackingId);
        auth.Verify(
            s => s.CallbackAndGenerateTokensAsync(
                It.IsAny<MarketplaceType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task LinkAsync_ShouldFailWithoutThrowing_WhenDtoIsNull()
    {
        var service = new IntegracaoLojaService(
            new Mock<IIntegracaoLojaRepository>().Object,
            new Mock<IUserAccountRepository>().Object,
            new Mock<IMarketplaceAccountRepository>().Object,
            new Mock<IMarketplaceAuthService>().Object,
            Production());

        var result = await service.LinkAsync(1, null!);

        Assert.False(result.Status);
        Assert.Equal("Payload de vinculação inválido.", result.Descricao);
    }

    [Fact]
    public async Task LinkAsync_ShouldPersistShopeeAccount_WhenAuthorizationCodeIsBlank()
    {
        var (service, stores, _, _) = CreateShopeeLinkMocks(userId: 1, shopKey: "ul-1-loja-homolog");
        IntegracaoLoja? saved = null;
        stores.Setup(repository => repository.AddAsync(It.IsAny<IntegracaoLoja>(), It.IsAny<CancellationToken>()))
            .Callback<IntegracaoLoja, CancellationToken>((entity, _) => saved = entity)
            .Returns(Task.CompletedTask);

        var result = await service.LinkAsync(1, new IntegracaoLojaDto
        {
            PlatformType = MarketplaceType.Shopee,
            AuthorizationCode = "   ",
            ShopId = null!,
            FriendlyName = "Loja Homolog"
        });

        Assert.True(result.Status);
        Assert.Equal("ul-1-loja-homolog", saved!.ShopId);
    }

    [Fact]
    public async Task LinkAsync_ShouldIgnoreSwappedShopId_WhenShopeeUniversalLink()
    {
        var (service, stores, auth, _) = CreateShopeeLinkMocks(userId: 1, shopKey: "ul-1-loja-homolog");
        IntegracaoLoja? saved = null;
        stores.Setup(repository => repository.AddAsync(It.IsAny<IntegracaoLoja>(), It.IsAny<CancellationToken>()))
            .Callback<IntegracaoLoja, CancellationToken>((entity, _) => saved = entity)
            .Returns(Task.CompletedTask);

        var result = await service.LinkAsync(1, new IntegracaoLojaDto
        {
            PlatformType = MarketplaceType.Shopee,
            AuthorizationCode = "123456",
            ShopId = "code_teste",
            FriendlyName = "Loja Homolog"
        });

        Assert.True(result.Status);
        Assert.Equal("ul-1-loja-homolog", saved!.ShopId);
        Assert.True(string.IsNullOrWhiteSpace(saved.AffiliateTrackingId));
        auth.Verify(
            s => s.CallbackAndGenerateTokensAsync(
                It.IsAny<MarketplaceType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task LinkAsync_ShouldAcceptSwappedStubPayload_WhenHomologacao()
    {
        var (service, stores, auth, _) = CreateShopeeLinkMocks(userId: 1, shopKey: "code_teste", homolog: true);
        stores.Setup(repository => repository.AddAsync(It.IsAny<IntegracaoLoja>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await service.LinkAsync(1, new IntegracaoLojaDto
        {
            PlatformType = MarketplaceType.Shopee,
            AuthorizationCode = "123456",
            ShopId = "code_teste",
            FriendlyName = "Loja Homolog"
        });

        Assert.True(result.Status);
        auth.Verify(
            s => s.CallbackAndGenerateTokensAsync(
                It.IsAny<MarketplaceType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task ListByUserAsync_ShouldReturnMarketplaceAccountsForUser()
    {
        var accounts = new Mock<IMarketplaceAccountRepository>();
        accounts.Setup(repository => repository.ListByUserIdAsync("7", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new MarketplaceAccount
                {
                    Id = 21,
                    UserId = "7",
                    TenantId = Guid.NewGuid(),
                    ShopId = "123456",
                    FriendlyName = "Loja Homolog",
                    ShopName = "Loja Homolog",
                    MarketplaceType = MarketplaceType.Shopee,
                    IsActive = true,
                    AccessToken = "token",
                    ExpiresAt = DateTime.UtcNow.AddDays(1),
                    CreatedAt = DateTime.UtcNow
                }
            ]);

        var stores = new Mock<IIntegracaoLojaRepository>();
        stores.Setup(repository => repository.ListByUserIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<IntegracaoLoja>());

        var service = new IntegracaoLojaService(
            stores.Object,
            new Mock<IUserAccountRepository>().Object,
            accounts.Object,
            new Mock<IMarketplaceAuthService>().Object,
            Production());

        var result = await service.ListByUserAsync(7, new IntegracaoLojaFilter { Page = 1, PageSize = 20 });

        Assert.True(result.Status);
        Assert.NotNull(result.DataList);
        Assert.Single(result.DataList);
        Assert.Equal("123456", result.DataList![0].ShopId);
        Assert.Equal("Loja Homolog", result.DataList[0].FriendlyName);
    }

    [Fact]
    public async Task LinkAsync_ShouldUseFirstExistingUser_WhenJwtUserIdIsMissing()
    {
        var fallbackUser = new UserAccount
        {
            Id = 3,
            Name = "Homolog",
            Email = "demo@tecso.local",
            PasswordHash = "hash",
            TenantId = Guid.NewGuid()
        };

        var users = new Mock<IUserAccountRepository>();
        users.Setup(repository => repository.GetByIdIgnoringFiltersAsync(99)).ReturnsAsync((UserAccount?)null);
        users.Setup(repository => repository.GetByIdAsync(99)).ReturnsAsync((UserAccount?)null);
        users.Setup(repository => repository.GetByIdIgnoringFiltersAsync(1)).ReturnsAsync((UserAccount?)null);
        users.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync((UserAccount?)null);
        users.Setup(repository => repository.GetFirstIgnoringFiltersAsync()).ReturnsAsync(fallbackUser);

        MarketplaceAccount? savedAccount = null;
        var accounts = new Mock<IMarketplaceAccountRepository>();
        accounts.Setup(repository => repository.GetByShopAsync("ul-3-achadinhos-de-aaz", MarketplaceType.Shopee))
            .ReturnsAsync((MarketplaceAccount?)null);
        accounts.Setup(repository => repository.UpsertAsync(It.IsAny<MarketplaceAccount>()))
            .Callback<MarketplaceAccount>(account => savedAccount = account)
            .Returns(Task.CompletedTask);

        IntegracaoLoja? saved = null;
        var stores = new Mock<IIntegracaoLojaRepository>();
        stores.Setup(repository => repository.GetByUserShopPlatformAsync(
                3, "ul-3-achadinhos-de-aaz", MarketplaceType.Shopee, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IntegracaoLoja?)null);
        stores.Setup(repository => repository.AddAsync(It.IsAny<IntegracaoLoja>(), It.IsAny<CancellationToken>()))
            .Callback<IntegracaoLoja, CancellationToken>((entity, _) => saved = entity)
            .Returns(Task.CompletedTask);

        var service = new IntegracaoLojaService(
            stores.Object,
            users.Object,
            accounts.Object,
            new Mock<IMarketplaceAuthService>().Object,
            Production());

        var result = await service.LinkAsync(99, new IntegracaoLojaDto
        {
            PlatformType = MarketplaceType.Shopee,
            FriendlyName = "@Achadinhos de Aaz",
            TrackingId = "18325850271"
        });

        Assert.True(result.Status);
        Assert.NotNull(savedAccount);
        Assert.Equal("3", savedAccount!.UserId);
        Assert.Equal("@Achadinhos de Aaz", savedAccount.FriendlyName);
        Assert.Equal("18325850271", savedAccount.TrackingId);
        Assert.Equal("18325850271", savedAccount.AffiliateTrackingId);
        Assert.Equal(3, saved!.UserId);
    }

    private static (
        IntegracaoLojaService Service,
        Mock<IIntegracaoLojaRepository> Stores,
        Mock<IMarketplaceAuthService> Auth,
        Mock<IMarketplaceAccountRepository> Accounts)
        CreateShopeeLinkMocks(int userId, string shopKey, bool homolog = false)
    {
        var user = new UserAccount
        {
            Id = userId,
            Name = "Demo",
            Email = "demo@tecso.local",
            PasswordHash = "hash",
            TenantId = Guid.NewGuid()
        };

        var users = new Mock<IUserAccountRepository>();
        users.Setup(repository => repository.GetByIdAsync(userId)).ReturnsAsync(user);

        var auth = new Mock<IMarketplaceAuthService>();
        var accounts = new Mock<IMarketplaceAccountRepository>();
        accounts.Setup(repository => repository.GetByShopAsync(shopKey, MarketplaceType.Shopee))
            .ReturnsAsync((MarketplaceAccount?)null);
        accounts.Setup(repository => repository.UpsertAsync(It.IsAny<MarketplaceAccount>()))
            .Returns(Task.CompletedTask);

        var stores = new Mock<IIntegracaoLojaRepository>();
        stores.Setup(repository => repository.GetByUserShopPlatformAsync(
                userId, shopKey, MarketplaceType.Shopee, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IntegracaoLoja?)null);

        var service = new IntegracaoLojaService(
            stores.Object,
            users.Object,
            accounts.Object,
            auth.Object,
            homolog ? Homolog() : Production());

        return (service, stores, auth, accounts);
    }

    private static IHostEnvironment Production() => Environment("Production");

    private static IHostEnvironment Homolog() => Environment("Homologacao");

    private static IHostEnvironment Environment(string name)
    {
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(item => item.EnvironmentName).Returns(name);
        return environment.Object;
    }
}
