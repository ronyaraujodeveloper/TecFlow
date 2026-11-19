using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Mail;
using Ofertio.Business.Service.PublicPages;
using Ofertio.Core.Entities;
using Ofertio.Database;
using Ofertio.Database.MultiTenancy;
using Ofertio.Infrastructure.Services.Email;
using Ofertio.Infrastructure.Services.PublicPages;
using Ofertio.Util.Security;
using Xunit;

namespace Ofertio.Tests.Unit.PublicPages;

public sealed class JoinLinkHealthServiceTests
{
    private const int UserId = 7;
    private const string Jid = "120363025246125486@g.us";
    private const string OldInvite = "https://chat.whatsapp.com/Ab12Cd34Ef56Gh78Ij90Kl";
    private const string NewInvite = "https://chat.whatsapp.com/Zz99Yy88Xx77Ww66Vv55Uu";
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly Mock<IEvolutionApiService> _evolution = new();
    private readonly Mock<ITelegramApiService> _telegram = new();
    private readonly Mock<ITransactionalEmailSender> _email = new();

    private static AppDbContext CreateContext()
    {
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(service => service.Encrypt(It.IsAny<string>())).Returns<string>(value => value ?? string.Empty);
        encryption.Setup(service => service.Decrypt(It.IsAny<string>())).Returns<string>(value => value ?? string.Empty);
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options, encryption.Object, new NullCurrentTenantService());
    }

    private async Task<(JoinLinkHealthService Service, AppDbContext Context, PublicPageJoinLink Link)> BuildAsync(
        string state = "open",
        GroupInviteResult? invite = null,
        int failedChecks = 0,
        DateTime? lastChecked = null)
    {
        var context = CreateContext();
        context.UserAccounts.Add(new UserAccount { Id = UserId, TenantId = TenantId, Name = "Rony", Email = "rony@ofertio.com", PasswordHash = "x" });
        var link = new PublicPageJoinLink
        {
            UserId = UserId, TenantId = TenantId, Channel = "WhatsApp", Label = "Achadinhos VIP", InviteUrl = OldInvite,
            SourceGroupId = Jid, Status = JoinLinkRules.StatusActive, FailedChecks = failedChecks, LastCheckedAt = lastChecked
        };
        context.PublicPageJoinLinks.Add(link);
        await context.SaveChangesAsync();

        _evolution.Setup(item => item.GetConnectionStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EvolutionConnectionStateDto { State = state });
        _evolution.Setup(item => item.FetchGroupInviteAsync(It.IsAny<string>(), Jid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite ?? new GroupInviteResult(GroupInviteOutcome.Ok, OldInvite));

        var service = new JoinLinkHealthService(
            context, _evolution.Object, _telegram.Object, _email.Object,
            Options.Create(new EmailOptions { PublicBaseUrl = "https://dev.ofertio.com.br" }),
            NullLogger<JoinLinkHealthService>.Instance);
        return (service, context, link);
    }

    [Fact]
    public async Task HealthyGroup_ShouldStayActiveAndRefreshTheInviteWhenItWasReset()
    {
        var (service, context, link) = await BuildAsync(invite: new GroupInviteResult(GroupInviteOutcome.Ok, NewInvite, 1500));
        await using var _ = context;

        var changed = await service.RunAsync();

        Assert.Equal(0, changed);
        await context.Entry(link).ReloadAsync();
        Assert.Equal(JoinLinkRules.StatusActive, link.Status);
        Assert.Equal(NewInvite, link.InviteUrl);
        Assert.Equal(1500, link.MemberCount);
        Assert.NotNull(link.LastCheckedAt);
    }

    [Fact]
    public async Task MissingGroup_ShouldOnlyDeactivateOnTheSecondConsecutiveFailure()
    {
        var (service, context, link) = await BuildAsync(invite: GroupInviteResult.NotFound);
        await using var _ = context;

        var first = await service.RunAsync();
        await context.Entry(link).ReloadAsync();
        Assert.Equal(0, first);
        Assert.Equal(JoinLinkRules.StatusActive, link.Status);
        Assert.Equal(1, link.FailedChecks);

        link.LastCheckedAt = DateTime.UtcNow.AddHours(-30);
        await context.SaveChangesAsync();
        var second = await service.RunAsync();

        await context.Entry(link).ReloadAsync();
        Assert.Equal(1, second);
        Assert.Equal(JoinLinkRules.StatusInactive, link.Status);
        Assert.NotNull(link.StatusReason);
    }

    [Fact]
    public async Task Deactivation_ShouldNotifyInTheSystemAndByEmail()
    {
        var (service, context, _) = await BuildAsync(invite: GroupInviteResult.NotFound, failedChecks: 1);
        await using var __ = context;

        await service.RunAsync();

        var notification = Assert.Single(context.TenantNotifications);
        Assert.Equal(TenantId, notification.TenantId);
        Assert.Contains("Achadinhos VIP", notification.Message);
        _email.Verify(item => item.SendAsync(
            It.Is<TransactionalEmail>(mail => mail.To == "rony@ofertio.com" && mail.TextBody.Contains("minhas-paginas-publicas")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WhatsAppDisconnected_ShouldMarkAsUnverifiedAndNeverDeactivate()
    {
        var (service, context, link) = await BuildAsync(state: "close", failedChecks: 1);
        await using var _ = context;

        var changed = await service.RunAsync();

        Assert.Equal(0, changed);
        await context.Entry(link).ReloadAsync();
        Assert.Equal(JoinLinkRules.StatusUnverified, link.Status);
        Assert.Equal(1, link.FailedChecks);
        _evolution.Verify(item => item.FetchGroupInviteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Empty(context.TenantNotifications);
    }

    [Fact]
    public async Task TransientError_ShouldNotCountAsAFailure()
    {
        var (service, context, link) = await BuildAsync(invite: GroupInviteResult.Unavailable, failedChecks: 1);
        await using var _ = context;

        await service.RunAsync();

        await context.Entry(link).ReloadAsync();
        Assert.Equal(JoinLinkRules.StatusUnverified, link.Status);
        Assert.Equal(1, link.FailedChecks);
    }

    [Fact]
    public async Task UnverifiedLink_ShouldRecoverWhenTheGroupAnswersAgain()
    {
        var (service, context, link) = await BuildAsync(failedChecks: 1);
        await using var _ = context;
        link.Status = JoinLinkRules.StatusUnverified;
        await context.SaveChangesAsync();

        await service.RunAsync();

        await context.Entry(link).ReloadAsync();
        Assert.Equal(JoinLinkRules.StatusActive, link.Status);
        Assert.Equal(0, link.FailedChecks);
    }

    [Fact]
    public async Task RecentlyCheckedAndInactiveLinks_ShouldBeSkipped()
    {
        var (service, context, link) = await BuildAsync(invite: GroupInviteResult.NotFound, lastChecked: DateTime.UtcNow.AddHours(-2));
        await using var _ = context;

        await service.RunAsync();
        Assert.Equal(0, (await context.PublicPageJoinLinks.AsNoTracking().SingleAsync()).FailedChecks);

        link.LastCheckedAt = null;
        link.Status = JoinLinkRules.StatusInactive;
        await context.SaveChangesAsync();
        await service.RunAsync();
        _evolution.Verify(item => item.FetchGroupInviteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ManualLinks_ShouldNeverBeChecked()
    {
        var (service, context, link) = await BuildAsync(invite: GroupInviteResult.NotFound);
        await using var _ = context;
        link.SourceGroupId = null;
        await context.SaveChangesAsync();

        await service.RunAsync();

        _evolution.Verify(item => item.FetchGroupInviteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TelegramLink_ShouldUseTheBotAndSkipWhenThereIsNoToken()
    {
        var (service, context, link) = await BuildAsync();
        await using var _ = context;
        link.Channel = "Telegram";
        link.SourceGroupId = "-1001234567";
        link.InviteUrl = "https://t.me/achadinhos_vip";
        await context.SaveChangesAsync();

        await service.RunAsync();
        await context.Entry(link).ReloadAsync();
        Assert.Equal(JoinLinkRules.StatusUnverified, link.Status);
        _telegram.Verify(item => item.GetChatInviteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        context.TelegramIntegrations.Add(new TelegramIntegration { UserId = UserId, Token = "123:abc" });
        link.LastCheckedAt = null;
        await context.SaveChangesAsync();
        _telegram.Setup(item => item.GetChatInviteAsync("123:abc", "-1001234567", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GroupInviteResult(GroupInviteOutcome.Ok, "https://t.me/achadinhos_vip", 900));

        await service.RunAsync();
        await context.Entry(link).ReloadAsync();
        Assert.Equal(JoinLinkRules.StatusActive, link.Status);
        Assert.Equal(900, link.MemberCount);
    }

    [Fact]
    public void ApplyResult_ShouldIgnoreAnInviteThatFailsValidation()
    {
        var link = new PublicPageJoinLink { Channel = "WhatsApp", InviteUrl = OldInvite, Status = JoinLinkRules.StatusActive };

        JoinLinkHealthService.ApplyResult(link, new GroupInviteResult(GroupInviteOutcome.Ok, "https://evil.com/x"));

        Assert.Equal(OldInvite, link.InviteUrl);
    }
}
