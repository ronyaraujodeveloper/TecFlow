using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database;
using TecFlow.Infrastructure.Services.Groups;
using TecFlow.Util.Security;

namespace TecFlow.Tests.Unit.Groups;

public class CouponReclassificationServiceTests
{
    [Fact]
    public async Task ReclassifyLegacyCouponsAsync_ShouldOnlyMoveCouponAnnouncements()
    {
        await using var context = CreateDbContext();
        context.GroupCapturedMessages.AddRange(
            NewMessage("announcement", "NOVO CUPOM MERCADO LIVRE\nCupom: CARE15 - 15% OFF em R$ 5, Limite de R$ 145 OFF\nhttps://meli.la/a"),
            NewMessage("product", "Purificador Consul\nPOR: R$ 391,42 no Pix\nUse o cupom: OFFHOJE\nhttps://meli.la/b"),
            NewMessage("plain", "Cadeira Gamer R$ 599,00\nhttps://meli.la/c"));
        await context.SaveChangesAsync();

        var service = new CouponReclassificationService(context, NullLogger<CouponReclassificationService>.Instance);

        var reclassified = await service.ReclassifyLegacyCouponsAsync();
        var secondRun = await service.ReclassifyLegacyCouponsAsync();

        Assert.Equal(1, reclassified);
        Assert.Equal(0, secondRun);
        var announcement = await context.GroupCapturedMessages.SingleAsync(x => x.ExternalMessageId == "announcement");
        Assert.Equal(OfferType.Coupon, announcement.Type);
        Assert.Equal("CARE15", announcement.CouponCode);
        Assert.Equal(OfferType.Product, (await context.GroupCapturedMessages.SingleAsync(x => x.ExternalMessageId == "product")).Type);
        Assert.Equal(OfferType.Product, (await context.GroupCapturedMessages.SingleAsync(x => x.ExternalMessageId == "plain")).Type);
    }

    private static GroupCapturedMessage NewMessage(string externalId, string rawText) =>
        new()
        {
            UserId = 7,
            Channel = "Telegram",
            GroupKey = "Telegram:1",
            GroupName = "Grupo",
            ExternalMessageId = externalId,
            RawText = rawText,
            OriginalUrl = "https://meli.la/" + externalId,
            HasDirectProductUrl = true,
            Type = OfferType.Product
        };

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
