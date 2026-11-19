using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Ofertio.Business.Configuration;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Attribution;
using Ofertio.Core.Entities;
using Ofertio.Core.Enums;
using Ofertio.Database;
using Ofertio.Database.Entity;
using Ofertio.Database.MultiTenancy;
using Ofertio.Infrastructure.Services.Analytics;
using Ofertio.SharedUi.Services.Dashboard;
using Ofertio.Util.Security;
using Xunit;

namespace Ofertio.Tests.Unit.Analytics;

public sealed class LinkAnalyticsServiceTests
{
    private const int UserId = 5;
    private const string Secret = "segredo-de-teste";
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OtherTenantId = Guid.NewGuid();
    private static readonly DateOnly Day = new(2026, 11, 10);

    private static AppDbContext CreateContext()
    {
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(service => service.Encrypt(It.IsAny<string>())).Returns<string>(value => value ?? string.Empty);
        encryption.Setup(service => service.Decrypt(It.IsAny<string>())).Returns<string>(value => value ?? string.Empty);
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options, encryption.Object, new NullCurrentTenantService());
    }

    private static LinkAnalyticsService Service(AppDbContext context) =>
        new(context, Options.Create(new ShortLinkOptions { GroupKeySecret = Secret }), NullLogger<LinkAnalyticsService>.Instance);

    private static ShortAffiliateLink Link(Guid tenant, Guid group, string name, string shopCode = "1") => new()
    {
        AffiliateLinkId = Guid.NewGuid(),
        LinkGroupId = group,
        ShortCode = Guid.NewGuid().ToString("N")[..7],
        DestinationUrl = "https://www.mercadolivre.com.br/p/MLB1",
        OriginalUrl = "https://www.mercadolivre.com.br/p/MLB1",
        PlatformType = MarketplaceType.MercadoLivre,
        ProductName = name,
        TenantId = tenant,
        UserId = UserId,
        IsActive = true
    };

    private static LinkClickLog Click(ShortAffiliateLink link, int hourUtc, string channel, bool bot = false, string? groupKey = null, string ip = "1.1.1.***", int day = 10, string shop = "97303724") => new()
    {
        AffiliateLinkId = link.AffiliateLinkId,
        TenantId = link.TenantId,
        ShopId = shop,
        OriginalUrl = link.OriginalUrl,
        ConvertedUrl = link.DestinationUrl,
        Platform = "Mercado Livre",
        EventKind = LinkClickLog.EventKindClick,
        ClickedAt = new DateTime(2026, 11, day, hourUtc, 0, 0, DateTimeKind.Utc),
        IpAddress = ip,
        UserAgent = "Chrome",
        DeviceType = "Mobile",
        Channel = channel,
        IsBot = bot,
        GroupKey = groupKey
    };

    private static AnalyticsRequest Request(Guid? tenant = null, string? shop = null) =>
        new(tenant ?? TenantId, UserId, Day, Day, AnalyticsGrain.Day, shop);

    [Fact]
    public async Task Overview_ShouldCountOnlyTheTenantsHumanClicksAndRankProducts()
    {
        await using var context = CreateContext();
        var productA = Guid.NewGuid();
        var productB = Guid.NewGuid();
        var linkA = Link(TenantId, productA, "Notebook");
        var linkB = Link(TenantId, productB, "Fone");
        var foreign = Link(OtherTenantId, Guid.NewGuid(), "De outro cliente");
        context.ShortAffiliateLinks.AddRange(linkA, linkB, foreign);
        context.LinkClickLogs.AddRange(
            Click(linkA, 15, "WhatsApp"),
            Click(linkA, 16, "WhatsApp", ip: "2.2.2.***"),
            Click(linkA, 17, "Telegram", ip: "3.3.3.***"),
            Click(linkB, 15, "WhatsApp", ip: "4.4.4.***"),
            Click(linkA, 18, "WhatsApp", bot: true, ip: "5.5.5.***"),
            Click(foreign, 15, "WhatsApp", ip: "6.6.6.***"));
        await context.SaveChangesAsync();

        var (data, error) = await Service(context).GetOverviewAsync(Request());

        Assert.Null(error);
        Assert.NotNull(data);
        Assert.Equal(4, data!.Summary.Clicks);
        Assert.Equal(1, data.Summary.PreviewsFiltered);
        Assert.Equal("Notebook", data.TopProducts[0].Name);
        Assert.Equal(3, data.TopProducts[0].Clicks);
        Assert.DoesNotContain(data.TopProducts, product => product.Name == "De outro cliente");
        Assert.True(data.HasAnyLink);
    }

    [Fact]
    public async Task Overview_ShouldFilterByShop()
    {
        await using var context = CreateContext();
        var link = Link(TenantId, Guid.NewGuid(), "Notebook");
        context.ShortAffiliateLinks.Add(link);
        context.LinkClickLogs.AddRange(
            Click(link, 15, "WhatsApp", shop: "loja-a"),
            Click(link, 16, "WhatsApp", shop: "loja-b", ip: "2.2.2.***"));
        await context.SaveChangesAsync();

        var (data, _) = await Service(context).GetOverviewAsync(Request(shop: "loja-a"));

        Assert.Equal(1, data!.Summary.Clicks);
    }

    [Fact]
    public async Task Overview_GeneratedLinks_ShouldFollowTheSameShopFilterAsTheClicks()
    {
        await using var context = CreateContext();
        var link = Link(TenantId, Guid.NewGuid(), "Notebook");
        context.ShortAffiliateLinks.Add(link);
        var generationA = Click(link, 15, "WhatsApp", shop: "loja-a");
        generationA.EventKind = LinkClickLog.EventKindGeneration;
        var generationB = Click(link, 16, "WhatsApp", shop: "loja-b");
        generationB.EventKind = LinkClickLog.EventKindGeneration;
        context.LinkClickLogs.AddRange(generationA, generationB, Click(link, 17, "WhatsApp", shop: "loja-a"));
        await context.SaveChangesAsync();

        var (all, _) = await Service(context).GetOverviewAsync(Request());
        var (onlyA, _) = await Service(context).GetOverviewAsync(Request(shop: "loja-a"));

        Assert.Equal(2, all!.Summary.LinksGenerated);
        Assert.Equal(1, onlyA!.Summary.LinksGenerated);
        Assert.Equal(1, onlyA.Summary.Clicks);
    }

    [Fact]
    public async Task Overview_ShouldResolveGroupNamesFromTheOpaqueKeyWithoutExposingTheJid()
    {
        await using var context = CreateContext();
        const string jid = "120363025246125486@g.us";
        context.WhatsAppGroups.Add(new WhatsAppGroup { UserId = UserId, Jid = jid, Name = "Achadinhos VIP", IsActive = true });
        var link = Link(TenantId, Guid.NewGuid(), "Notebook");
        context.ShortAffiliateLinks.Add(link);
        var key = GroupKeyRules.Compute(Secret, UserId, "WhatsApp", jid);
        context.LinkClickLogs.Add(Click(link, 15, "WhatsApp", groupKey: key));
        await context.SaveChangesAsync();

        var (data, _) = await Service(context).GetOverviewAsync(Request());

        var group = Assert.Single(data!.Groups);
        Assert.Equal("Achadinhos VIP", group.Name);
        Assert.Equal(key, group.Key);
        Assert.DoesNotContain(jid, group.Key + group.Name);
    }

    [Fact]
    public async Task Overview_ForATenantWithoutLinks_ShouldSayThereAreNoLinks()
    {
        await using var context = CreateContext();

        var (data, error) = await Service(context).GetOverviewAsync(Request());

        Assert.Null(error);
        Assert.False(data!.HasAnyLink);
        Assert.Equal(0, data.Summary.Clicks);
        Assert.Single(data.Series);
    }

    [Fact]
    public async Task Overview_ShouldRejectInvalidPeriodsAndMissingTenant()
    {
        await using var context = CreateContext();
        var service = Service(context);

        var (_, invalidPeriod) = await service.GetOverviewAsync(Request() with { From = Day.AddDays(2), To = Day });
        var (_, noTenant) = await service.GetOverviewAsync(Request(tenant: Guid.Empty));

        Assert.NotNull(invalidPeriod);
        Assert.NotNull(noTenant);
    }

    [Fact]
    public async Task Product_ShouldReturnTheSeriesOfJustThatProduct()
    {
        await using var context = CreateContext();
        var productA = Guid.NewGuid();
        var linkA = Link(TenantId, productA, "Notebook");
        var linkB = Link(TenantId, Guid.NewGuid(), "Fone");
        context.ShortAffiliateLinks.AddRange(linkA, linkB);
        context.LinkClickLogs.AddRange(
            Click(linkA, 15, "Telegram"),
            Click(linkA, 16, "WhatsApp", ip: "2.2.2.***"),
            Click(linkB, 15, "WhatsApp", ip: "3.3.3.***"));
        await context.SaveChangesAsync();

        var (data, error) = await Service(context).GetProductAsync(Request(), productA);

        Assert.Null(error);
        Assert.Equal("Notebook", data!.Product.Name);
        Assert.Equal(2, data.Summary.Clicks);
    }

    [Fact]
    public async Task Product_FromAnotherTenantOrUnknown_ShouldNotBeFound()
    {
        await using var context = CreateContext();
        var foreignProduct = Guid.NewGuid();
        var foreign = Link(OtherTenantId, foreignProduct, "Segredo");
        context.ShortAffiliateLinks.Add(foreign);
        context.LinkClickLogs.Add(Click(foreign, 15, "WhatsApp"));
        await context.SaveChangesAsync();

        var (data, error) = await Service(context).GetProductAsync(Request(), foreignProduct);

        Assert.Null(data);
        Assert.NotNull(error);
    }
}

public sealed class PeriodPresetsTests
{
    private static readonly DateOnly Today = new(2026, 11, 18);

    [Fact]
    public void Resolve_ShouldBuildEachPreset()
    {
        Assert.Equal(new PeriodSelection(Today, Today, AnalyticsGrain.Day), PeriodPresets.Resolve(PeriodPresets.Today, Today));
        Assert.Equal(new PeriodSelection(new DateOnly(2026, 11, 12), Today, AnalyticsGrain.Day), PeriodPresets.Resolve(PeriodPresets.Last7, Today));
        Assert.Equal(new PeriodSelection(new DateOnly(2026, 10, 20), Today, AnalyticsGrain.Day), PeriodPresets.Resolve(PeriodPresets.Last30, Today));
        Assert.Equal(new PeriodSelection(new DateOnly(2025, 12, 1), Today, AnalyticsGrain.Month), PeriodPresets.Resolve(PeriodPresets.Last12Months, Today));
    }

    [Fact]
    public void KeyFor_ShouldRecognizePresetsAndFallBackToCustom()
    {
        Assert.Equal(PeriodPresets.Last7, PeriodPresets.KeyFor(PeriodPresets.Resolve(PeriodPresets.Last7, Today), Today));
        Assert.Equal(PeriodPresets.Custom, PeriodPresets.KeyFor(new PeriodSelection(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 5), AnalyticsGrain.Day), Today));
    }

    [Fact]
    public void FromQuery_ShouldRoundTripAndFallBackWhenInvalid()
    {
        var selection = new PeriodSelection(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 14), AnalyticsGrain.Week);
        var parts = PeriodPresets.ToQuery(selection).Split('&').Select(part => part.Split('=')).ToDictionary(pair => pair[0], pair => pair[1]);

        Assert.Equal(selection, PeriodPresets.FromQuery(parts["from"], parts["to"], parts["grain"], Today));
        Assert.Equal(PeriodPresets.Resolve(PeriodPresets.Last30, Today), PeriodPresets.FromQuery("lixo", null, null, Today));
        Assert.Equal(PeriodPresets.Resolve(PeriodPresets.Last30, Today), PeriodPresets.FromQuery("2026-11-14", "2026-11-01", "day", Today));
    }
}
