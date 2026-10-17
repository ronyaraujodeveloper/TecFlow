using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Entities;
using TecFlow.Database;
using TecFlow.Infrastructure.Services.Groups;
using TecFlow.Util.Security;

namespace TecFlow.Tests.Unit.Groups;

public class OfferRevalidationServiceTests
{
    [Fact]
    public async Task RevalidateActiveOffersAsync_ShouldOnlyCheckDueActiveOffersAndCountDeactivated()
    {
        await using var context = CreateDbContext();
        var now = DateTime.UtcNow;
        context.GroupCapturedMessages.AddRange(
            NewOffer("https://a.test/1", GroupOfferStatuses.Ativo, lastValidatedAt: null),
            NewOffer("https://a.test/2", GroupOfferStatuses.PrecoAlterado, lastValidatedAt: now.AddHours(-3)),
            NewOffer("https://a.test/recent", GroupOfferStatuses.Ativo, lastValidatedAt: now.AddMinutes(-10)),
            NewOffer("https://a.test/sold", GroupOfferStatuses.Esgotado, lastValidatedAt: null, isAvailable: false),
            NewOffer("https://a.test/ignored", GroupOfferStatuses.Ativo, lastValidatedAt: null, isIgnored: true));
        await context.SaveChangesAsync();

        var checkedUrls = new List<string>();
        var liveCheck = new Mock<ILiveCheckSearchService>();
        liveCheck
            .Setup(x => x.CheckAndPersistAsync(It.IsAny<GroupCapturedMessage>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns<GroupCapturedMessage, int, CancellationToken>(async (entity, _, ct) =>
            {
                checkedUrls.Add(entity.OriginalUrl);
                if (entity.OriginalUrl.EndsWith("/1", StringComparison.Ordinal))
                {
                    entity.OfferStatus = GroupOfferStatuses.VitrineGenerica;
                    entity.IsAvailable = false;
                }

                entity.LastValidatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(ct);
                return new OfficialOfferSnapshotDto { IsAvailable = entity.IsAvailable, Status = entity.OfferStatus };
            });

        var service = new OfferRevalidationService(
            context,
            liveCheck.Object,
            NullLogger<OfferRevalidationService>.Instance);

        var summary = await service.RevalidateActiveOffersAsync(50, TimeSpan.FromHours(2), TimeSpan.Zero);

        Assert.Equal(2, summary.Checked);
        Assert.Equal(1, summary.Deactivated);
        Assert.Equal(["https://a.test/1", "https://a.test/2"], checkedUrls);
        var storefront = await context.GroupCapturedMessages.SingleAsync(x => x.OriginalUrl == "https://a.test/1");
        Assert.False(storefront.IsAvailable);
        Assert.Equal(GroupOfferStatuses.VitrineGenerica, storefront.OfferStatus);
    }

    private static GroupCapturedMessage NewOffer(
        string url,
        string status,
        DateTime? lastValidatedAt,
        bool isAvailable = true,
        bool isIgnored = false) =>
        new()
        {
            UserId = 7,
            Channel = "Telegram",
            GroupKey = "Telegram:1",
            GroupName = "Grupo",
            OriginalUrl = url,
            OfferStatus = status,
            LastValidatedAt = lastValidatedAt,
            IsAvailable = isAvailable,
            IsIgnored = isIgnored
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
