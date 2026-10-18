using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TecFlow.Business.Integrations.MercadoLivre;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database;
using TecFlow.Infrastructure.Services.LinkStrategies;
using TecFlow.Util.Security;

namespace TecFlow.Tests.Unit.LinkStrategies;

public class AffiliateUrlBuilderServiceTests
{
    [Fact]
    public void Build_ShouldAppendMattToolToMercadoLivreUrl()
    {
        var url = AffiliateUrlBuilderRules.Build(
            "https://produto.mercadolivre.com.br/MLB-123456789-smart-tv-_JM",
            null,
            MarketplaceType.MercadoLivre,
            "97303724");

        Assert.Contains("matt_tool=97303724", url, StringComparison.Ordinal);
        Assert.StartsWith("https://produto.mercadolivre.com.br/MLB-123456789-smart-tv-_JM?", url, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_ShouldReplaceExistingMattToolAndKeepOtherParameters()
    {
        var url = AffiliateUrlBuilderRules.Build(
            "https://www.mercadolivre.com.br/p/MLB123?matt_tool=11111111&color=azul",
            null,
            null,
            "97303724");

        var query = new Uri(url).Query;
        Assert.Contains("matt_tool=97303724", query, StringComparison.Ordinal);
        Assert.DoesNotContain("11111111", query, StringComparison.Ordinal);
        Assert.Contains("color=azul", query, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_ShouldPreferDirectProductUrl_WhenOriginalIsShortLink()
    {
        var url = AffiliateUrlBuilderRules.Build(
            "https://meli.la/2abcXYZ",
            "https://www.mercadolivre.com.br/p/MLB999",
            MarketplaceType.MercadoLivre,
            "97303724");

        Assert.StartsWith("https://www.mercadolivre.com.br/p/MLB999?", url, StringComparison.Ordinal);
        Assert.Contains("matt_tool=97303724", url, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_ShouldKeepOtherMarketplacesUntouched()
    {
        const string shopee = "https://shopee.com.br/produto-i.1.2";

        Assert.Equal(shopee, AffiliateUrlBuilderRules.Build(shopee, null, MarketplaceType.Shopee, "97303724"));
    }

    [Fact]
    public async Task ResolveMercadoLivreMattToolAsync_ShouldUseTenantAccountTrackingId()
    {
        await using var context = CreateDbContext();
        var tenantId = Guid.NewGuid();
        context.UserAccounts.Add(new UserAccount { Id = 5, TenantId = tenantId, Name = "Rony", Email = "rony@test.local", PasswordHash = "x" });
        context.MarketplaceAccounts.Add(new MarketplaceAccount
        {
            TenantId = tenantId,
            UserId = "5",
            MarketplaceType = MarketplaceType.MercadoLivre,
            FriendlyName = "AchadinhoCerto",
            TrackingId = "97303724",
            IsActive = true
        });
        await context.SaveChangesAsync();

        var service = CreateService(context, fallback: "00000000");

        Assert.Equal("97303724", await service.ResolveMercadoLivreMattToolAsync(5));
    }

    [Fact]
    public async Task ResolveMercadoLivreMattToolAsync_ShouldFallbackToConfiguredDefault()
    {
        await using var context = CreateDbContext();

        var service = CreateService(context, fallback: "97303724");

        Assert.Equal("97303724", await service.ResolveMercadoLivreMattToolAsync(42));
    }

    private static AffiliateUrlBuilderService CreateService(AppDbContext context, string fallback)
    {
        var options = new Mock<IOptionsMonitor<MercadoLivreIntegrationOptions>>();
        options.Setup(x => x.CurrentValue).Returns(new MercadoLivreIntegrationOptions { DefaultAffiliateTrackingId = fallback });
        return new AffiliateUrlBuilderService(context, options.Object, NullLogger<AffiliateUrlBuilderService>.Instance);
    }

    private static AppDbContext CreateDbContext()
    {
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns<string>(s => s);
        encryption.Setup(e => e.Decrypt(It.IsAny<string>())).Returns<string>(s => s);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options, encryption.Object, new TecFlow.Database.MultiTenancy.NullCurrentTenantService());
    }
}
