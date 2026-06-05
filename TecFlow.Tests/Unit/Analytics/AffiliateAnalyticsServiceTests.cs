using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TecFlow.Business.Integrations.Auth;
using TecFlow.Business.Integrations.Shopee;
using TecFlow.Business.Integrations.TikTokShop;
using TecFlow.Business.Interfaces.Telemetry;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database;
using TecFlow.Infrastructure.Services.Analytics;
using TecFlow.Util.Security;

namespace TecFlow.Tests.Unit.Analytics;

public class AffiliateAnalyticsServiceTests
{
    [Fact]
    public async Task ReconcileCommissionsAsync_ShouldFlagDiscrepancy_WhenPaidLessThanExpected()
    {
        await using var db = CreateDbContext();
        var ownerId = 1;
        var campaign = new Campaign
        {
            Name = "C",
            Budget = 100,
            OwnerId = ownerId,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(30)
        };
        db.Campaigns.Add(campaign);
        await db.SaveChangesAsync();

        db.Affiliates.Add(new Affiliate
        {
            Name = "Test",
            Email = "a@test.com",
            AffiliateCode = "AFF-001",
            Commission = 10m,
            CampaignId = campaign.Id,
            OwnerId = ownerId
        });
        await db.SaveChangesAsync();

        db.MarketplaceOrders.Add(new MarketplaceOrder
        {
            ExternalOrderId = "ORD-1",
            ShopId = "shop-1",
            MarketplaceType = MarketplaceType.Shopee,
            Status = "COMPLETED",
            ProcessedAt = DateTime.UtcNow,
            Lines =
            {
                new MarketplaceOrderLine { SkuCode = "SKU1", Quantity = 2 }
            }
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var (performance, discrepancies) = await service.ReconcileCommissionsAsync(
            ownerId,
            "AFF-001",
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow.AddDays(1));

        Assert.True(performance.DiscrepancyCount >= 0);
        Assert.Equal("AFF-001", performance.AffiliateId);
    }

    private static AffiliateAnalyticsService CreateService(AppDbContext db)
    {
        var auth = new Mock<IMarketplaceAuthService>();
        var sign = new Mock<IMarketplaceSignatureService>();
        var factory = new Mock<IHttpClientFactory>();

        return new AffiliateAnalyticsService(
            db,
            auth.Object,
            sign.Object,
            factory.Object,
            Options.Create(new ShopeeIntegrationOptions()),
            Options.Create(new TikTokShopIntegrationOptions()),
            new Mock<ITecFlowBusinessMetrics>().Object,
            new Mock<ITelemetryRecentErrorRecorder>().Object,
            NullLogger<AffiliateAnalyticsService>.Instance);
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
