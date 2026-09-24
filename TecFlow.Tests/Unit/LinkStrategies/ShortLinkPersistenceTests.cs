using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TecFlow.Business.Configuration;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database;
using TecFlow.Database.Entity;
using TecFlow.Infrastructure.Services.Repositories;
using TecFlow.Infrastructure.Services.ShortLinks;
using TecFlow.Util.Security;

namespace TecFlow.Tests.Unit.LinkStrategies;

public class ShortLinkPersistenceTests
{
    private static readonly Guid TestTenantId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    [Fact]
    public async Task ShortLinkService_ShouldPersistAffiliateUrlAndMarketplaceAccountId_OnSqlMappedTable()
    {
        await using var db = CreateDbContext();
        var account = new MarketplaceAccount
        {
            TenantId = TestTenantId,
            UserId = "10",
            ShopId = "ul-10-loja-homolog",
            FriendlyName = "Loja Homolog",
            ShopName = "Loja Homolog",
            MarketplaceType = MarketplaceType.Shopee,
            AccessToken = "token",
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow
        };
        db.MarketplaceAccounts.Add(account);
        var loja = new IntegracaoLoja
        {
            UserId = 10,
            TenantId = TestTenantId,
            ShopId = "ul-10-loja-homolog",
            FriendlyName = "Loja Homolog",
            AccessToken = "token",
            PlatformType = MarketplaceType.Shopee,
            CreatedAt = DateTime.UtcNow
        };
        db.IntegracaoLojas.Add(loja);
        await db.SaveChangesAsync();

        var service = new ShortLinkService(
            new ShortAffiliateLinkRepository(db),
            db,
            Options.Create(new ShortLinkOptions()),
            NullLogger<ShortLinkService>.Instance);

        const string original = "https://shopee.com.br/produto-i.123.456";
        const string affiliate = "https://shopee.com.br/universal-link/product/123/456?sub_id=6512300000";

        var (publicUrl, affiliateLinkId) = await service.CreateShortLinkAsync(
            affiliate,
            original,
            MarketplaceType.Shopee,
            userId: 10,
            tenantId: TestTenantId,
            integracaoLojaId: loja.Id,
            customNickname: null);

        var saved = Assert.Single(db.ShortAffiliateLinks);
        Assert.Equal(original, saved.OriginalUrl);
        Assert.Equal(affiliate, saved.AffiliateUrl);
        Assert.Equal(affiliate, saved.DestinationUrl);
        Assert.False(string.IsNullOrWhiteSpace(saved.ShortCode));
        Assert.Equal(saved.ShortCode, saved.Code);
        Assert.Equal(MarketplaceType.Shopee, saved.Platform);
        Assert.Equal(account.Id, saved.MarketplaceAccountId);
        Assert.Equal(DateTime.UtcNow.Date, saved.CreatedAt.Date);
        Assert.Contains(saved.ShortCode, publicUrl, StringComparison.Ordinal);
        Assert.Contains("LojaHomolog", publicUrl, StringComparison.Ordinal);
        Assert.DoesNotContain("/r/", publicUrl, StringComparison.Ordinal);
        Assert.Equal(saved.AffiliateLinkId, affiliateLinkId);
        Assert.NotEqual(Guid.Empty, saved.LinkGroupId);
        Assert.True(saved.IsActive);
        var association = Assert.Single(db.ShortAffiliateLinkAccounts);
        Assert.Equal(saved.Id, association.ShortAffiliateLinkId);
        Assert.Equal(loja.Id, association.IntegracaoLojaId);
        Assert.True(association.IsActive);
    }

    [Fact]
    public async Task ShortLinkService_ShouldKeepInactiveAccountRow_WhenUnselected()
    {
        await using var db = CreateDbContext();
        var first = await SeedStoreAsync(db, "loja-a", "Loja A");
        var second = await SeedStoreAsync(db, "loja-b", "Loja B");
        var service = CreateService(db);
        const string original = "https://shopee.com.br/produto-i.123.456";

        var groupId = await service.ResolveLinkGroupIdAsync(10, original, MarketplaceType.Shopee);
        await service.EnsureForStoreAsync(
            "https://shopee.com.br/a",
            original,
            MarketplaceType.Shopee,
            10,
            TestTenantId,
            first.Loja.Id,
            groupId,
            null);
        await service.EnsureForStoreAsync(
            "https://shopee.com.br/b",
            original,
            MarketplaceType.Shopee,
            10,
            TestTenantId,
            second.Loja.Id,
            groupId,
            null);

        await service.DeactivateUnselectedAccountsAsync(groupId, [first.Loja.Id]);

        Assert.Equal(2, db.ShortAffiliateLinks.Count());
        Assert.Equal(2, db.ShortAffiliateLinkAccounts.Count());
        Assert.True(db.ShortAffiliateLinks.Single(link => link.IntegracaoLojaId == first.Loja.Id).IsActive);
        Assert.False(db.ShortAffiliateLinks.Single(link => link.IntegracaoLojaId == second.Loja.Id).IsActive);
        Assert.False(db.ShortAffiliateLinkAccounts.Single(account => account.IntegracaoLojaId == second.Loja.Id).IsActive);
        Assert.True(db.ShortAffiliateLinkAccounts.Single(account => account.IntegracaoLojaId == first.Loja.Id).IsActive);
    }

    private static ShortLinkService CreateService(AppDbContext db) =>
        new(
            new ShortAffiliateLinkRepository(db),
            db,
            Options.Create(new ShortLinkOptions()),
            NullLogger<ShortLinkService>.Instance);

    private static async Task<(MarketplaceAccount Account, IntegracaoLoja Loja)> SeedStoreAsync(
        AppDbContext db,
        string shopId,
        string friendlyName)
    {
        var account = new MarketplaceAccount
        {
            TenantId = TestTenantId,
            UserId = "10",
            ShopId = shopId,
            FriendlyName = friendlyName,
            ShopName = friendlyName,
            MarketplaceType = MarketplaceType.Shopee,
            AccessToken = "token",
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow
        };
        db.MarketplaceAccounts.Add(account);
        var loja = new IntegracaoLoja
        {
            UserId = 10,
            TenantId = TestTenantId,
            ShopId = shopId,
            FriendlyName = friendlyName,
            AccessToken = "token",
            PlatformType = MarketplaceType.Shopee,
            CreatedAt = DateTime.UtcNow
        };
        db.IntegracaoLojas.Add(loja);
        await db.SaveChangesAsync();
        return (account, loja);
    }

    private static AppDbContext CreateDbContext()
    {
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns<string>(s => s ?? string.Empty);
        encryption.Setup(e => e.Decrypt(It.IsAny<string>())).Returns<string>(s => s ?? string.Empty);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options, encryption.Object, new TecFlow.Database.MultiTenancy.NullCurrentTenantService());
    }
}
