using Microsoft.EntityFrameworkCore;
using Moq;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Master;
using Ofertio.Core.Entities;
using Ofertio.Database;
using Ofertio.Database.MultiTenancy;
using Ofertio.Infrastructure.Services.Master;
using Ofertio.Util.Security;

namespace Ofertio.Tests.Unit.Master;

public class PlanScheduleGuardTests
{
    private const int UserId = 7;

    private static AppDbContext CreateContext()
    {
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(service => service.Encrypt(It.IsAny<string>())).Returns<string>(value => value ?? string.Empty);
        encryption.Setup(service => service.Decrypt(It.IsAny<string>())).Returns<string>(value => value ?? string.Empty);
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options, encryption.Object, new NullCurrentTenantService());
    }

    private static IPlanLimitsService LimitOf(int maxDailySchedules)
    {
        var limits = new Mock<IPlanLimitsService>();
        limits.Setup(service => service.GetForUserAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlanLimits(SaasPlanCodes.Starter, 1, 0, false, maxDailySchedules));
        return limits.Object;
    }

    private static async Task<AppDbContext> SeedAsync(string? stateCode, DateTime scheduledAtUtc)
    {
        var context = CreateContext();
        context.UserAccounts.Add(new UserAccount
        {
            Id = UserId,
            Name = "Cliente",
            Email = "cliente@ofertio.com",
            PasswordHash = "hash",
            StateCode = stateCode
        });
        context.WhatsAppBroadcastCampaigns.Add(new WhatsAppBroadcastCampaign
        {
            UserId = UserId,
            Title = "Manhã",
            MessageText = "Oferta",
            ScheduledAt = scheduledAtUtc
        });
        await context.SaveChangesAsync();
        return context;
    }

    [Fact]
    public async Task CheckAsync_ShouldCountLateEveningInTheSameBrasiliaDay()
    {
        var morningUtc = new DateTime(2026, 10, 26, 13, 0, 0, DateTimeKind.Utc);
        await using var context = await SeedAsync(null, morningUtc);
        var lateEveningUtc = new DateTime(2026, 10, 27, 1, 0, 0, DateTimeKind.Utc);

        var message = await PlanScheduleGuard.CheckAsync(LimitOf(1), context, UserId, lateEveningUtc, CancellationToken.None);

        Assert.NotNull(message);
        Assert.Contains("disparo", message);
    }

    [Fact]
    public async Task CheckAsync_ShouldReleaseTheNextBrasiliaDay()
    {
        var morningUtc = new DateTime(2026, 10, 26, 13, 0, 0, DateTimeKind.Utc);
        await using var context = await SeedAsync(null, morningUtc);
        var nextDayUtc = new DateTime(2026, 10, 27, 3, 30, 0, DateTimeKind.Utc);

        var message = await PlanScheduleGuard.CheckAsync(LimitOf(1), context, UserId, nextDayUtc, CancellationToken.None);

        Assert.Null(message);
    }

    [Fact]
    public async Task CheckAsync_ShouldUseTheClientStateDay()
    {
        var acreEveningUtc = new DateTime(2026, 10, 27, 4, 0, 0, DateTimeKind.Utc);
        await using var context = await SeedAsync("AC", acreEveningUtc);
        var acreMorningUtc = new DateTime(2026, 10, 26, 14, 0, 0, DateTimeKind.Utc);

        var message = await PlanScheduleGuard.CheckAsync(LimitOf(1), context, UserId, acreMorningUtc, CancellationToken.None);

        Assert.NotNull(message);
    }

    [Fact]
    public async Task CheckAsync_ShouldIgnoreUnlimitedPlans()
    {
        var morningUtc = new DateTime(2026, 10, 26, 13, 0, 0, DateTimeKind.Utc);
        await using var context = await SeedAsync(null, morningUtc);

        var message = await PlanScheduleGuard.CheckAsync(LimitOf(0), context, UserId, morningUtc, CancellationToken.None);

        Assert.Null(message);
    }
}
