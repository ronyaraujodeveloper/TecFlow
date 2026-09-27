using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TecFlow.Business.Integrations;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database.Entity;
using TecFlow.Infrastructure.Services.LinkStrategies;

namespace TecFlow.Tests.Unit.LinkStrategies;

public class IntegracaoLojaScopeResolverTests
{
    [Fact]
    public async Task ResolveAsync_ShouldUseMarketplaceAccountId_WhenIntegracaoLojaIdDiffers()
    {
        var account = CreateAccount(id: 42, shopId: "shopee-shop", platform: MarketplaceType.Shopee);
        var loja = CreateLoja(id: 7, shopId: "shopee-shop", platform: MarketplaceType.Shopee);
        var resolver = CreateResolver(
            lojaById: null,
            accountById: account,
            accounts: [account],
            lojas: [loja],
            lojaByShop: loja);

        var resolved = await resolver.ResolveAsync(
            IntegracaoLojaScopeHelper.EncodeStoreScope(42),
            userId: 10,
            MarketplaceType.Shopee);

        Assert.Equal(7, resolved.Id);
        Assert.Equal("shopee-shop", resolved.ShopId);
    }

    [Fact]
    public async Task ResolveAsync_ShouldPickFirstActiveShopee_WhenScopeIsEmptyOrOtherPlatform()
    {
        var magalu = CreateAccount(id: 1, shopId: "magalu-shop", platform: MarketplaceType.MagazineLuiza);
        var shopee = CreateAccount(id: 2, shopId: "shopee-shop", platform: MarketplaceType.Shopee);
        var loja = CreateLoja(id: 9, shopId: "shopee-shop", platform: MarketplaceType.Shopee);
        var resolver = CreateResolver(
            lojaById: CreateLoja(id: 1, shopId: "magalu-shop", platform: MarketplaceType.MagazineLuiza),
            accountById: magalu,
            accounts: [magalu, shopee],
            lojas: [loja],
            lojaByShop: loja);

        var resolved = await resolver.ResolveAsync(Guid.Empty, userId: 10, MarketplaceType.Shopee);

        Assert.Equal(9, resolved.Id);
        Assert.Equal(MarketplaceType.Shopee, resolved.PlatformType);
    }

    [Fact]
    public async Task ResolveAsync_ShouldFindMagaluAccountByAliasAndTracking5321952()
    {
        var magalu = CreateAccount(id: 3, shopId: "magalu-shop", platform: MarketplaceType.MagazineLuiza);
        magalu.TrackingId = "5321952";
        magalu.AffiliateTrackingId = "5321952";
        magalu.FriendlyName = "Magalu";
        var olderMagalu = CreateAccount(id: 2, shopId: "magalu-old", platform: MarketplaceType.MagazineLuiza);
        olderMagalu.CreatedAt = DateTime.UtcNow.AddDays(-2);
        var loja = CreateLoja(id: 11, shopId: "magalu-shop", platform: MarketplaceType.MagazineLuiza);
        loja.AffiliateTrackingId = "5321952";
        var resolver = CreateResolver(
            lojaById: null,
            accountById: magalu,
            accounts: [olderMagalu, magalu],
            lojas: [loja],
            lojaByShop: loja);

        var resolved = await resolver.ResolveAsync(Guid.Empty, userId: 10, MarketplaceType.MagazineLuiza);

        Assert.Equal(11, resolved.Id);
        Assert.Equal("5321952", resolved.AffiliateTrackingId);
        Assert.True(MarketplaceTypeExtensions.AreSamePlatform(MarketplaceType.MagazineLuiza, "Magalu"));
        Assert.True(MarketplaceTypeExtensions.AreSamePlatform(MarketplaceType.MagazineLuiza, "Magazine Luiza"));
        Assert.True(MarketplaceTypeExtensions.AreSamePlatform(MarketplaceType.MagazineLuiza, "MagazineLuiza"));
        Assert.True(MarketplaceTypeExtensions.AreSamePlatform(MarketplaceType.MagazineLuiza, "Magazine_Luiza"));
        Assert.Equal(MarketplaceType.MagazineLuiza, AffiliateTrackingIdValidator.ParsePlatform("Magalu"));
        Assert.Equal(MarketplaceType.MagazineLuiza, AffiliateTrackingIdValidator.ParsePlatform("Magazine_Luiza"));
    }

    [Fact]
    public async Task ResolveAsync_ShouldThrowFriendlyMessage_WhenUserHasNoActiveAccountForPlatform()
    {
        var magalu = CreateAccount(id: 1, shopId: "magalu-shop", platform: MarketplaceType.MagazineLuiza);
        var resolver = CreateResolver(
            lojaById: null,
            accountById: magalu,
            accounts: [magalu],
            lojas: [],
            lojaByShop: null);

        var ex = await Assert.ThrowsAsync<AffiliateLinkGenerationException>(() =>
            resolver.ResolveAsync(Guid.Empty, userId: 10, MarketplaceType.Shopee));

        Assert.Equal(
            IntegracaoLojaScopeResolver.MissingConnectedAccountMessage(MarketplaceType.Shopee),
            ex.Message);
        Assert.Contains("Shopee", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("escopo selecionado", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static IntegracaoLojaScopeResolver CreateResolver(
        IntegracaoLoja? lojaById,
        MarketplaceAccount? accountById,
        IReadOnlyList<MarketplaceAccount> accounts,
        IReadOnlyList<IntegracaoLoja> lojas,
        IntegracaoLoja? lojaByShop)
    {
        var lojaRepo = new Mock<IIntegracaoLojaRepository>();
        lojaRepo.Setup(repository => repository.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lojaById);
        lojaRepo.Setup(repository => repository.ListByUserIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lojas);
        lojaRepo.Setup(repository => repository.GetByUserShopPlatformAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<MarketplaceType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(lojaByShop);

        var accountRepo = new Mock<IMarketplaceAccountRepository>();
        accountRepo.Setup(repository => repository.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountById);
        accountRepo.Setup(repository => repository.ListByUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts);

        return new IntegracaoLojaScopeResolver(
            lojaRepo.Object,
            accountRepo.Object,
            NullLogger<IntegracaoLojaScopeResolver>.Instance);
    }

    private static MarketplaceAccount CreateAccount(int id, string shopId, MarketplaceType platform) =>
        new()
        {
            Id = id,
            UserId = "10",
            ShopId = shopId,
            FriendlyName = shopId,
            MarketplaceType = platform,
            IsActive = true,
            AccessToken = "token",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            TenantId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")
        };

    private static IntegracaoLoja CreateLoja(int id, string shopId, MarketplaceType platform) =>
        new()
        {
            Id = id,
            UserId = 10,
            ShopId = shopId,
            FriendlyName = shopId,
            PlatformType = platform,
            AccessToken = "token",
            Status = MarketplaceIntegrationStatus.Active,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            TenantId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")
        };
}
