using Microsoft.Extensions.Hosting;
using Moq;
using TecFlow.Business.Dto;
using TecFlow.Business.Integrations;
using TecFlow.Business.Integrations.Auth;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database.Entity;
using TecFlow.Database.Filter;
using TecFlow.Infrastructure.Services.Integrations;
using TecFlow.Infrastructure.Services.Tenancy;

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

        var service = new IntegracaoLojaService(stores.Object, users.Object, accounts.Object, auth.Object, AccountPrep(users), Production());
        var result = await service.LinkAsync(7, new IntegracaoLojaDto
        {
            PlatformType = MarketplaceType.Shopee,
            TrackingId = "6512300000",
            FriendlyName = "Loja Homolog"
        });

        Assert.True(result.Status);
        Assert.Contains("Universal Link", result.Descricao, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(saved);
        Assert.Equal("ul-7-loja-homolog", saved!.ShopId);
        Assert.Equal("6512300000", saved.AffiliateTrackingId);
        Assert.Equal("Loja Homolog", saved.FriendlyName);
        Assert.NotNull(savedAccount);
        Assert.Equal("ul-7-loja-homolog", savedAccount!.ShopId);
        Assert.Equal("6512300000", savedAccount.AffiliateTrackingId);
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
    public async Task LinkAsync_ShouldFail_WhenTrackingIdAlreadyActiveOnSamePlatform()
    {
        var (service, _, auth, accounts) = CreateShopeeLinkMocks(userId: 7, shopKey: "ul-7-loja-homolog");
        accounts.Setup(repository => repository.ExistsActiveTrackingIdAsync(
                MarketplaceType.Shopee,
                "6512300000",
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await service.LinkAsync(7, new IntegracaoLojaDto
        {
            PlatformType = MarketplaceType.Shopee,
            TrackingId = "6512300000",
            FriendlyName = "Loja Duplicada"
        });

        Assert.False(result.Status);
        Assert.Equal(
            AffiliateTrackingIdValidator.DuplicateTrackingIdMessage(MarketplaceType.Shopee),
            result.Descricao);
        accounts.Verify(repository => repository.UpsertAsync(It.IsAny<MarketplaceAccount>()), Times.Never);
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
    public async Task LinkAsync_ShouldPersistTikTokShopAccount_WithFriendlyNameAndTrackingId()
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

        var accounts = new Mock<IMarketplaceAccountRepository>();
        accounts.Setup(repository => repository.GetByShopAsync("ul-tt-1-loja-tiktok", MarketplaceType.TikTokShop))
            .ReturnsAsync((MarketplaceAccount?)null);
        MarketplaceAccount? savedAccount = null;
        accounts.Setup(repository => repository.UpsertAsync(It.IsAny<MarketplaceAccount>()))
            .Callback<MarketplaceAccount>(account => savedAccount = account)
            .Returns(Task.CompletedTask);

        IntegracaoLoja? saved = null;
        var stores = new Mock<IIntegracaoLojaRepository>();
        stores.Setup(repository => repository.GetByUserShopPlatformAsync(
                1, "ul-tt-1-loja-tiktok", MarketplaceType.TikTokShop, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IntegracaoLoja?)null);
        stores.Setup(repository => repository.AddAsync(It.IsAny<IntegracaoLoja>(), It.IsAny<CancellationToken>()))
            .Callback<IntegracaoLoja, CancellationToken>((entity, _) => saved = entity)
            .Returns(Task.CompletedTask);

        var auth = new Mock<IMarketplaceAuthService>();
        var service = new IntegracaoLojaService(
            stores.Object,
            users.Object,
            accounts.Object,
            auth.Object,
            AccountPrep(users),
            Production());

        var result = await service.LinkAsync(1, new IntegracaoLojaDto
        {
            PlatformType = MarketplaceType.TikTokShop,
            TrackingId = "6512300000",
            FriendlyName = "Loja TikTok"
        });

        Assert.True(result.Status);
        Assert.Contains("TikTok Shop", result.Descricao, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("ul-tt-1-loja-tiktok", saved!.ShopId);
        Assert.Equal(MarketplaceType.TikTokShop, saved.PlatformType);
        Assert.Equal("6512300000", saved.AffiliateTrackingId);
        Assert.Equal(MarketplaceType.TikTokShop, savedAccount!.MarketplaceType);
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
    public async Task LinkAsync_ShouldPersistMercadoLivreAccount_WithFriendlyNameAndMattToolId()
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

        var accounts = new Mock<IMarketplaceAccountRepository>();
        accounts.Setup(repository => repository.GetByShopAsync("ul-ml-1-loja-ml", MarketplaceType.MercadoLivre))
            .ReturnsAsync((MarketplaceAccount?)null);
        MarketplaceAccount? savedAccount = null;
        accounts.Setup(repository => repository.UpsertAsync(It.IsAny<MarketplaceAccount>()))
            .Callback<MarketplaceAccount>(account => savedAccount = account)
            .Returns(Task.CompletedTask);

        IntegracaoLoja? saved = null;
        var stores = new Mock<IIntegracaoLojaRepository>();
        stores.Setup(repository => repository.GetByUserShopPlatformAsync(
                1, "ul-ml-1-loja-ml", MarketplaceType.MercadoLivre, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IntegracaoLoja?)null);
        stores.Setup(repository => repository.AddAsync(It.IsAny<IntegracaoLoja>(), It.IsAny<CancellationToken>()))
            .Callback<IntegracaoLoja, CancellationToken>((entity, _) => saved = entity)
            .Returns(Task.CompletedTask);

        var auth = new Mock<IMarketplaceAuthService>();
        var service = new IntegracaoLojaService(
            stores.Object,
            users.Object,
            accounts.Object,
            auth.Object,
            AccountPrep(users),
            Production());

        var result = await service.LinkAsync(1, new IntegracaoLojaDto
        {
            PlatformType = MarketplaceType.MercadoLivre,
            TrackingId = "987654321",
            FriendlyName = "Loja ML"
        });

        Assert.True(result.Status);
        Assert.Contains("Mercado Livre", result.Descricao, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("ul-ml-1-loja-ml", saved!.ShopId);
        Assert.Equal(MarketplaceType.MercadoLivre, saved.PlatformType);
        Assert.Equal("987654321", saved.AffiliateTrackingId);
        Assert.Equal(MarketplaceType.MercadoLivre, savedAccount!.MarketplaceType);
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
    public async Task LinkAsync_ShouldPersistAmazonAccount_WithFriendlyNameAndAssociateTag()
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

        var accounts = new Mock<IMarketplaceAccountRepository>();
        accounts.Setup(repository => repository.GetByShopAsync("ul-am-1-loja-amazon", MarketplaceType.Amazon))
            .ReturnsAsync((MarketplaceAccount?)null);
        MarketplaceAccount? savedAccount = null;
        accounts.Setup(repository => repository.UpsertAsync(It.IsAny<MarketplaceAccount>()))
            .Callback<MarketplaceAccount>(account => savedAccount = account)
            .Returns(Task.CompletedTask);

        IntegracaoLoja? saved = null;
        var stores = new Mock<IIntegracaoLojaRepository>();
        stores.Setup(repository => repository.GetByUserShopPlatformAsync(
                1, "ul-am-1-loja-amazon", MarketplaceType.Amazon, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IntegracaoLoja?)null);
        stores.Setup(repository => repository.AddAsync(It.IsAny<IntegracaoLoja>(), It.IsAny<CancellationToken>()))
            .Callback<IntegracaoLoja, CancellationToken>((entity, _) => saved = entity)
            .Returns(Task.CompletedTask);

        var auth = new Mock<IMarketplaceAuthService>();
        var service = new IntegracaoLojaService(
            stores.Object,
            users.Object,
            accounts.Object,
            auth.Object,
            AccountPrep(users),
            Production());

        var result = await service.LinkAsync(1, new IntegracaoLojaDto
        {
            PlatformType = MarketplaceType.Amazon,
            TrackingId = "sualoja-20",
            FriendlyName = "Loja Amazon"
        });

        Assert.True(result.Status);
        Assert.Contains("Amazon", result.Descricao, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("ul-am-1-loja-amazon", saved!.ShopId);
        Assert.Equal(MarketplaceType.Amazon, saved.PlatformType);
        Assert.Equal("sualoja-20", saved.AffiliateTrackingId);
        Assert.Equal(MarketplaceType.Amazon, savedAccount!.MarketplaceType);
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
    public async Task LinkAsync_ShouldPersistMagazineLuizaAccount_WithFriendlyNameAndPartnerStore()
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

        var accounts = new Mock<IMarketplaceAccountRepository>();
        accounts.Setup(repository => repository.GetByShopAsync("ul-mg-1-loja-magalu", MarketplaceType.MagazineLuiza))
            .ReturnsAsync((MarketplaceAccount?)null);
        MarketplaceAccount? savedAccount = null;
        accounts.Setup(repository => repository.UpsertAsync(It.IsAny<MarketplaceAccount>()))
            .Callback<MarketplaceAccount>(account => savedAccount = account)
            .Returns(Task.CompletedTask);

        IntegracaoLoja? saved = null;
        var stores = new Mock<IIntegracaoLojaRepository>();
        stores.Setup(repository => repository.GetByUserShopPlatformAsync(
                1, "ul-mg-1-loja-magalu", MarketplaceType.MagazineLuiza, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IntegracaoLoja?)null);
        stores.Setup(repository => repository.AddAsync(It.IsAny<IntegracaoLoja>(), It.IsAny<CancellationToken>()))
            .Callback<IntegracaoLoja, CancellationToken>((entity, _) => saved = entity)
            .Returns(Task.CompletedTask);

        var auth = new Mock<IMarketplaceAuthService>();
        var service = new IntegracaoLojaService(
            stores.Object,
            users.Object,
            accounts.Object,
            auth.Object,
            AccountPrep(users),
            Production());

        var result = await service.LinkAsync(1, new IntegracaoLojaDto
        {
            PlatformType = MarketplaceType.MagazineLuiza,
            TrackingId = "magazinematos",
            FriendlyName = "Loja Magalu"
        });

        Assert.True(result.Status);
        Assert.Contains("Magazine Luiza", result.Descricao, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("ul-mg-1-loja-magalu", saved!.ShopId);
        Assert.Equal(MarketplaceType.MagazineLuiza, saved.PlatformType);
        Assert.Equal("magazinematos", saved.AffiliateTrackingId);
        Assert.Equal(MarketplaceType.MagazineLuiza, savedAccount!.MarketplaceType);
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
    public async Task LinkAsync_ShouldPersistKabumAccount_WithFriendlyNameAndTrackingId()
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

        var accounts = new Mock<IMarketplaceAccountRepository>();
        accounts.Setup(repository => repository.GetByShopAsync("ul-kb-1-loja-kabum", MarketplaceType.Kabum))
            .ReturnsAsync((MarketplaceAccount?)null);
        MarketplaceAccount? savedAccount = null;
        accounts.Setup(repository => repository.UpsertAsync(It.IsAny<MarketplaceAccount>()))
            .Callback<MarketplaceAccount>(account => savedAccount = account)
            .Returns(Task.CompletedTask);

        IntegracaoLoja? saved = null;
        var stores = new Mock<IIntegracaoLojaRepository>();
        stores.Setup(repository => repository.GetByUserShopPlatformAsync(
                1, "ul-kb-1-loja-kabum", MarketplaceType.Kabum, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IntegracaoLoja?)null);
        stores.Setup(repository => repository.AddAsync(It.IsAny<IntegracaoLoja>(), It.IsAny<CancellationToken>()))
            .Callback<IntegracaoLoja, CancellationToken>((entity, _) => saved = entity)
            .Returns(Task.CompletedTask);

        var auth = new Mock<IMarketplaceAuthService>();
        var service = new IntegracaoLojaService(
            stores.Object,
            users.Object,
            accounts.Object,
            auth.Object,
            AccountPrep(users),
            Production());

        var result = await service.LinkAsync(1, new IntegracaoLojaDto
        {
            PlatformType = MarketplaceType.Kabum,
            TrackingId = "tecflow_kabum",
            FriendlyName = "Loja Kabum"
        });

        Assert.True(result.Status);
        Assert.Contains("Kabum", result.Descricao, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("ul-kb-1-loja-kabum", saved!.ShopId);
        Assert.Equal(MarketplaceType.Kabum, saved.PlatformType);
        Assert.Equal("tecflow_kabum", saved.AffiliateTrackingId);
        Assert.Equal(MarketplaceType.Kabum, savedAccount!.MarketplaceType);
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
    public async Task LinkAsync_ShouldPersistCasasBahiaAccount_WithFriendlyNameAndParceiroId()
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

        var accounts = new Mock<IMarketplaceAccountRepository>();
        accounts.Setup(repository => repository.GetByShopAsync("ul-cb-1-loja-cb", MarketplaceType.CasasBahia))
            .ReturnsAsync((MarketplaceAccount?)null);
        MarketplaceAccount? savedAccount = null;
        accounts.Setup(repository => repository.UpsertAsync(It.IsAny<MarketplaceAccount>()))
            .Callback<MarketplaceAccount>(account => savedAccount = account)
            .Returns(Task.CompletedTask);

        IntegracaoLoja? saved = null;
        var stores = new Mock<IIntegracaoLojaRepository>();
        stores.Setup(repository => repository.GetByUserShopPlatformAsync(
                1, "ul-cb-1-loja-cb", MarketplaceType.CasasBahia, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IntegracaoLoja?)null);
        stores.Setup(repository => repository.AddAsync(It.IsAny<IntegracaoLoja>(), It.IsAny<CancellationToken>()))
            .Callback<IntegracaoLoja, CancellationToken>((entity, _) => saved = entity)
            .Returns(Task.CompletedTask);

        var auth = new Mock<IMarketplaceAuthService>();
        var service = new IntegracaoLojaService(
            stores.Object,
            users.Object,
            accounts.Object,
            auth.Object,
            AccountPrep(users),
            Production());

        var result = await service.LinkAsync(1, new IntegracaoLojaDto
        {
            PlatformType = MarketplaceType.CasasBahia,
            TrackingId = "tecflow_cb",
            FriendlyName = "Loja CB"
        });

        Assert.True(result.Status);
        Assert.Contains("Casas Bahia", result.Descricao, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("ul-cb-1-loja-cb", saved!.ShopId);
        Assert.Equal(MarketplaceType.CasasBahia, saved.PlatformType);
        Assert.Equal("tecflow_cb", saved.AffiliateTrackingId);
        Assert.Equal(MarketplaceType.CasasBahia, savedAccount!.MarketplaceType);
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
        var users = new Mock<IUserAccountRepository>();
        var service = new IntegracaoLojaService(
            new Mock<IIntegracaoLojaRepository>().Object,
            users.Object,
            new Mock<IMarketplaceAccountRepository>().Object,
            new Mock<IMarketplaceAuthService>().Object,
            AccountPrep(users),
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

        var users = new Mock<IUserAccountRepository>();
        var service = new IntegracaoLojaService(
            stores.Object,
            users.Object,
            accounts.Object,
            new Mock<IMarketplaceAuthService>().Object,
            AccountPrep(users),
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
            AccountPrep(users),
            Production());

        var result = await service.LinkAsync(99, new IntegracaoLojaDto
        {
            PlatformType = MarketplaceType.Shopee,
            FriendlyName = "@Achadinhos de Aaz",
            TrackingId = "6512300000"
        });

        Assert.True(result.Status);
        Assert.NotNull(savedAccount);
        Assert.Equal("3", savedAccount!.UserId);
        Assert.Equal("@Achadinhos de Aaz", savedAccount.FriendlyName);
        Assert.Equal("6512300000", savedAccount.TrackingId);
        Assert.Equal("6512300000", savedAccount.AffiliateTrackingId);
        Assert.Equal(3, saved!.UserId);
        Assert.Equal(fallbackUser.TenantId, savedAccount.TenantId);
        Assert.NotEqual(Guid.Empty, savedAccount.TenantId);
    }

    [Fact]
    public async Task UnlinkAsync_ShouldInactivateAccountById_WithoutUpsert()
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
        users.Setup(repository => repository.GetByIdIgnoringFiltersAsync(7)).ReturnsAsync(user);
        users.Setup(repository => repository.GetByIdAsync(7)).ReturnsAsync(user);

        var account = new MarketplaceAccount
        {
            Id = 42,
            UserId = "7",
            ShopId = "ul-7-loja-homolog",
            MarketplaceType = MarketplaceType.Shopee,
            IsActive = true,
            TenantId = user.TenantId
        };

        var accounts = new Mock<IMarketplaceAccountRepository>();
        accounts.Setup(repository => repository.GetByIdAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        accounts.Setup(repository => repository.SetInactiveByIdAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var stores = new Mock<IIntegracaoLojaRepository>();
        stores.Setup(repository => repository.GetByIdAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IntegracaoLoja?)null);

        var service = new IntegracaoLojaService(
            stores.Object,
            users.Object,
            accounts.Object,
            new Mock<IMarketplaceAuthService>().Object,
            AccountPrep(users, accounts),
            Production());

        var result = await service.UnlinkAsync(7, 42);

        Assert.True(result.Status);
        Assert.Equal("Loja desconectada com sucesso!", result.Descricao);
        accounts.Verify(repository => repository.SetInactiveByIdAsync(42, It.IsAny<CancellationToken>()), Times.Once);
        accounts.Verify(repository => repository.UpsertAsync(It.IsAny<MarketplaceAccount>()), Times.Never);
    }

    private static MarketplaceAccountService AccountPrep(
        Mock<IUserAccountRepository> users,
        Mock<IMarketplaceAccountRepository>? accounts = null)
    {
        var tenants = new Mock<ITenantProvisioningService>();
        tenants.Setup(service => service.EnsureTenantForUserAsync(It.IsAny<UserAccount>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserAccount user, CancellationToken _) =>
            {
                if (user.TenantId == Guid.Empty)
                {
                    user.TenantId = TenantProvisioningService.DefaultTenantId;
                }

                return new Tenant
                {
                    Id = user.TenantId,
                    Name = "Tenant Principal",
                    IsActive = true
                };
            });
        tenants.Setup(service => service.EnsurePersistedTenantAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid? preferred, CancellationToken _) => new Tenant
            {
                Id = preferred is { } id && id != Guid.Empty ? id : TenantProvisioningService.DefaultTenantId,
                Name = "Tenant Principal",
                IsActive = true
            });

        return new MarketplaceAccountService(
            tenants.Object,
            users.Object,
            (accounts ?? new Mock<IMarketplaceAccountRepository>()).Object);
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
            AccountPrep(users, accounts),
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
