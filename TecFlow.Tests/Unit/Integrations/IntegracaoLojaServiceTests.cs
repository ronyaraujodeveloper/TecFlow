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
                "123456",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MarketplaceTokenResult
            {
                Success = true,
                Descricao = "OK",
                ShopId = "123456",
                MarketplaceType = MarketplaceType.Shopee,
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            });

        var accounts = new Mock<IMarketplaceAccountRepository>();
        accounts.Setup(repository => repository.GetByShopAsync("123456", MarketplaceType.Shopee))
            .ReturnsAsync(new MarketplaceAccount
            {
                TenantId = user.TenantId,
                ShopId = "123456",
                ShopName = "123456",
                MarketplaceType = MarketplaceType.Shopee,
                AccessToken = HomologMarketplaceAuth.StubAccessToken,
                RefreshToken = HomologMarketplaceAuth.StubRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            });
        accounts.Setup(repository => repository.UpsertAsync(It.IsAny<MarketplaceAccount>()))
            .Returns(Task.CompletedTask);

        IntegracaoLoja? saved = null;
        var stores = new Mock<IIntegracaoLojaRepository>();
        stores.Setup(repository => repository.GetByUserShopPlatformAsync(
                7, "123456", MarketplaceType.Shopee, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IntegracaoLoja?)null);
        stores.Setup(repository => repository.AddAsync(It.IsAny<IntegracaoLoja>(), It.IsAny<CancellationToken>()))
            .Callback<IntegracaoLoja, CancellationToken>((entity, _) => saved = entity)
            .Returns(Task.CompletedTask);

        var service = new IntegracaoLojaService(stores.Object, users.Object, accounts.Object, auth.Object, Production());
        var result = await service.LinkAsync(7, new IntegracaoLojaDto
        {
            PlatformType = MarketplaceType.Shopee,
            AuthorizationCode = "code_teste",
            ShopId = "123456",
            FriendlyName = "Loja Homolog"
        });

        Assert.True(result.Status);
        Assert.Equal(HomologMarketplaceAuth.ManualLinkSuccessMessage, result.Descricao);
        Assert.NotNull(saved);
        Assert.Equal("123456", saved!.ShopId);
        Assert.Equal(HomologMarketplaceAuth.StubAccessToken, saved.AccessToken);
        auth.Verify(
            service => service.CallbackAndGenerateTokensAsync(
                MarketplaceType.Shopee,
                HomologMarketplaceAuth.StubAuthorizationCode,
                "123456",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LinkAsync_ShouldFail_WhenShopeeShopIdIsNotNumeric()
    {
        var service = new IntegracaoLojaService(
            new Mock<IIntegracaoLojaRepository>().Object,
            new Mock<IUserAccountRepository>().Object,
            new Mock<IMarketplaceAccountRepository>().Object,
            new Mock<IMarketplaceAuthService>().Object,
            Production());

        var result = await service.LinkAsync(1, new IntegracaoLojaDto
        {
            PlatformType = MarketplaceType.Shopee,
            AuthorizationCode = "code_teste",
            ShopId = "loja-abc",
            FriendlyName = "Loja Homolog"
        });

        Assert.False(result.Status);
        Assert.Contains("número inteiro", result.Descricao, StringComparison.OrdinalIgnoreCase);
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
    public async Task LinkAsync_ShouldFailWithoutThrowing_WhenAuthorizationCodeAndShopIdAreSwapped()
    {
        var auth = new Mock<IMarketplaceAuthService>();
        var service = new IntegracaoLojaService(
            new Mock<IIntegracaoLojaRepository>().Object,
            new Mock<IUserAccountRepository>().Object,
            new Mock<IMarketplaceAccountRepository>().Object,
            auth.Object,
            Production());

        var result = await service.LinkAsync(1, new IntegracaoLojaDto
        {
            PlatformType = MarketplaceType.Shopee,
            AuthorizationCode = "123456",
            ShopId = "code_teste",
            FriendlyName = "Loja Homolog"
        });

        Assert.False(result.Status);
        Assert.Contains("número inteiro", result.Descricao, StringComparison.OrdinalIgnoreCase);
        auth.Verify(
            service => service.CallbackAndGenerateTokensAsync(
                It.IsAny<MarketplaceType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task LinkAsync_ShouldFailWithoutThrowing_WhenCodeAndShopIdAreBlank()
    {
        var service = new IntegracaoLojaService(
            new Mock<IIntegracaoLojaRepository>().Object,
            new Mock<IUserAccountRepository>().Object,
            new Mock<IMarketplaceAccountRepository>().Object,
            new Mock<IMarketplaceAuthService>().Object,
            Production());

        var result = await service.LinkAsync(1, new IntegracaoLojaDto
        {
            PlatformType = MarketplaceType.Shopee,
            AuthorizationCode = "   ",
            ShopId = null!,
            FriendlyName = "Loja Homolog"
        });

        Assert.False(result.Status);
        Assert.Equal("Código de autorização OAuth é obrigatório.", result.Descricao);
    }

    [Fact]
    public async Task LinkAsync_ShouldAcceptSwappedStubPayload_WhenHomologacao()
    {
        var user = new UserAccount
        {
            Id = 1,
            Name = "Demo",
            Email = "demo@tecso.local",
            PasswordHash = "hash",
            TenantId = Guid.NewGuid()
        };

        var users = new Mock<IUserAccountRepository>();
        users.Setup(repository => repository.GetByIdAsync(1)).ReturnsAsync(user);

        var auth = new Mock<IMarketplaceAuthService>();
        auth.Setup(service => service.CallbackAndGenerateTokensAsync(
                MarketplaceType.Shopee,
                HomologMarketplaceAuth.StubAuthorizationCode,
                "123456",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MarketplaceTokenResult { Success = true, ShopId = "123456", MarketplaceType = MarketplaceType.Shopee, ExpiresAt = DateTime.UtcNow.AddDays(1) });

        var accounts = new Mock<IMarketplaceAccountRepository>();
        accounts.Setup(repository => repository.GetByShopAsync("123456", MarketplaceType.Shopee))
            .ReturnsAsync(new MarketplaceAccount
            {
                TenantId = user.TenantId,
                ShopId = "123456",
                ShopName = "123456",
                MarketplaceType = MarketplaceType.Shopee,
                AccessToken = HomologMarketplaceAuth.StubAccessToken,
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            });
        accounts.Setup(repository => repository.UpsertAsync(It.IsAny<MarketplaceAccount>()))
            .Returns(Task.CompletedTask);

        var stores = new Mock<IIntegracaoLojaRepository>();
        stores.Setup(repository => repository.GetByUserShopPlatformAsync(
                1, "123456", MarketplaceType.Shopee, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IntegracaoLoja?)null);
        stores.Setup(repository => repository.AddAsync(It.IsAny<IntegracaoLoja>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new IntegracaoLojaService(stores.Object, users.Object, accounts.Object, auth.Object, Homolog());
        var result = await service.LinkAsync(1, new IntegracaoLojaDto
        {
            PlatformType = MarketplaceType.Shopee,
            AuthorizationCode = "123456",
            ShopId = "code_teste",
            FriendlyName = "Loja Homolog"
        });

        Assert.True(result.Status);
        auth.Verify(
            service => service.CallbackAndGenerateTokensAsync(
                MarketplaceType.Shopee,
                HomologMarketplaceAuth.StubAuthorizationCode,
                "123456",
                It.IsAny<CancellationToken>()),
            Times.Once);
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

    private static IHostEnvironment Production() => Environment("Production");

    private static IHostEnvironment Homolog() => Environment("Homologacao");

    private static IHostEnvironment Environment(string name)
    {
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(item => item.EnvironmentName).Returns(name);
        return environment.Object;
    }
}
