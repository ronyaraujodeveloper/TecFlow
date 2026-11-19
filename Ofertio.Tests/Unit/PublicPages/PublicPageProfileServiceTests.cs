using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Master;
using Ofertio.Business.Service.PublicPages;
using Ofertio.Core.Entities;
using Ofertio.Database;
using Ofertio.Database.MultiTenancy;
using Ofertio.Infrastructure.Services.PublicPages;
using Ofertio.Util.Security;
using Xunit;

namespace Ofertio.Tests.Unit.PublicPages;

public sealed class PublicPageProfileServiceTests : IDisposable
{
    private const int UserId = 7;
    private const string Jid = "120363025246125486@g.us";
    private const string WhatsAppInvite = "https://chat.whatsapp.com/Ab12Cd34Ef56Gh78Ij90Kl";
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly string _uploadsRoot = Path.Combine(Path.GetTempPath(), "ofertio-tests-" + Guid.NewGuid().ToString("N"));
    private readonly Mock<IEvolutionApiService> _evolution = new();
    private readonly Mock<ITelegramApiService> _telegram = new();
    private int _maxJoinLinks = 2;

    public void Dispose()
    {
        if (Directory.Exists(_uploadsRoot))
        {
            Directory.Delete(_uploadsRoot, true);
        }
    }

    private static AppDbContext CreateContext()
    {
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(service => service.Encrypt(It.IsAny<string>())).Returns<string>(value => value ?? string.Empty);
        encryption.Setup(service => service.Decrypt(It.IsAny<string>())).Returns<string>(value => value ?? string.Empty);
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options, encryption.Object, new NullCurrentTenantService());
    }

    private async Task<(PublicPageProfileService Service, AppDbContext Context)> BuildAsync(bool withGroup = true)
    {
        var context = CreateContext();
        context.UserAccounts.Add(new UserAccount { Id = UserId, TenantId = TenantId, Name = "Rony", Email = "rony@ofertio.com", PasswordHash = "x" });
        if (withGroup)
        {
            context.WhatsAppGroups.Add(new WhatsAppGroup { UserId = UserId, Jid = Jid, Name = "Achadinhos VIP", ParticipantCount = 1250, IsAdmin = true, IsActive = true });
            context.WhatsAppGroups.Add(new WhatsAppGroup { UserId = UserId, Jid = "outro@g.us", Name = "Sem admin", ParticipantCount = 10, IsAdmin = false, IsActive = true });
        }

        await context.SaveChangesAsync();

        var uploads = new Mock<IUploadStorage>();
        uploads.SetupGet(item => item.UploadsRoot).Returns(_uploadsRoot);
        var limits = new Mock<IPlanLimitsService>();
        limits.Setup(item => item.GetForUserAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new PlanLimits("Starter", 0, 0, true, 0, _maxJoinLinks));
        _evolution.Setup(item => item.GetConnectionStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EvolutionConnectionStateDto { State = "open" });
        _evolution.Setup(item => item.FetchGroupInviteAsync(It.IsAny<string>(), Jid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GroupInviteResult(GroupInviteOutcome.Ok, WhatsAppInvite));

        return (new PublicPageProfileService(context, uploads.Object, limits.Object, _evolution.Object, _telegram.Object, NullLogger<PublicPageProfileService>.Instance), context);
    }

    [Fact]
    public async Task CreateFromGroup_ShouldBuildTheInviteLabelAndMemberCount()
    {
        var (service, context) = await BuildAsync();
        await using var _ = context;

        var result = await service.CreateJoinLinkAsync(UserId, new JoinLinkCreateDto { Channel = "WhatsApp", GroupId = Jid, ShowMemberCount = true });

        Assert.True(result.Status, result.Descricao);
        var link = Assert.Single(result.Data!.JoinLinks);
        Assert.Equal("Achadinhos VIP", link.Label);
        Assert.Equal(WhatsAppInvite, link.InviteUrl);
        Assert.True(link.IsVerifiable);
        Assert.True(link.ShowMemberCount);
        Assert.Equal(1250, link.MemberCount);
        Assert.True(result.Data.AvailableGroups.Single(group => group.GroupId == Jid).AlreadyUsed);
    }

    [Fact]
    public async Task CreateFromGroup_ShouldRefuseGroupsWhereTheAccountIsNotAdmin()
    {
        var (service, context) = await BuildAsync();
        await using var _ = context;

        var result = await service.CreateJoinLinkAsync(UserId, new JoinLinkCreateDto { Channel = "WhatsApp", GroupId = "outro@g.us" });

        Assert.False(result.Status);
        Assert.Contains("administradora", result.Descricao);
        _evolution.Verify(item => item.FetchGroupInviteAsync(It.IsAny<string>(), "outro@g.us", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateFromGroup_ShouldNotUseAGroupOfAnotherUser()
    {
        var (service, context) = await BuildAsync();
        await using var _ = context;
        context.WhatsAppGroups.Add(new WhatsAppGroup { UserId = 99, Jid = "alheio@g.us", Name = "Alheio", IsAdmin = true, IsActive = true });
        await context.SaveChangesAsync();

        var result = await service.CreateJoinLinkAsync(UserId, new JoinLinkCreateDto { Channel = "WhatsApp", GroupId = "alheio@g.us" });

        Assert.False(result.Status);
    }

    [Fact]
    public async Task CreateFromGroup_ShouldExplainWhenWhatsAppIsDisconnectedOrInviteUnavailable()
    {
        var (service, context) = await BuildAsync();
        await using var _ = context;
        _evolution.Setup(item => item.GetConnectionStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EvolutionConnectionStateDto { State = "close" });

        var disconnected = await service.CreateJoinLinkAsync(UserId, new JoinLinkCreateDto { Channel = "WhatsApp", GroupId = Jid });
        Assert.False(disconnected.Status);
        Assert.Contains("conectado", disconnected.Descricao);

        _evolution.Setup(item => item.GetConnectionStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EvolutionConnectionStateDto { State = "open" });
        _evolution.Setup(item => item.FetchGroupInviteAsync(It.IsAny<string>(), Jid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GroupInviteResult.NotFound);
        var notFound = await service.CreateJoinLinkAsync(UserId, new JoinLinkCreateDto { Channel = "WhatsApp", GroupId = Jid });
        Assert.False(notFound.Status);
    }

    [Fact]
    public async Task Create_ShouldEnforceThePlanLimitAndTreatZeroAsUnlimited()
    {
        var (service, context) = await BuildAsync(withGroup: false);
        await using var _ = context;
        _maxJoinLinks = 2;

        var first = await service.CreateJoinLinkAsync(UserId, new JoinLinkCreateDto { Channel = "WhatsApp", InviteUrl = WhatsAppInvite });
        var second = await service.CreateJoinLinkAsync(UserId, new JoinLinkCreateDto { Channel = "Telegram", InviteUrl = "https://t.me/achadinhos_vip" });
        var third = await service.CreateJoinLinkAsync(UserId, new JoinLinkCreateDto { Channel = "Telegram", InviteUrl = "https://t.me/+AbCdEfGhIjKl12" });

        Assert.True(first.Status);
        Assert.True(second.Status);
        Assert.False(third.Status);
        Assert.Contains("2 link", third.Descricao);
        Assert.False(second.Data!.CanAddJoinLink);

        _maxJoinLinks = 0;
        var unlimited = await service.CreateJoinLinkAsync(UserId, new JoinLinkCreateDto { Channel = "Telegram", InviteUrl = "https://t.me/+AbCdEfGhIjKl12" });
        Assert.True(unlimited.Status);
    }

    [Theory]
    [InlineData("WhatsApp", "https://evil.com/Ab12Cd34Ef56Gh78Ij90Kl")]
    [InlineData("WhatsApp", "https://wa.me/5511999999999")]
    [InlineData("Telegram", "javascript:alert(1)")]
    [InlineData("Discord", "https://discord.gg/abc12345")]
    public async Task CreateManual_ShouldRejectInvalidInvites(string channel, string url)
    {
        var (service, context) = await BuildAsync(withGroup: false);
        await using var _ = context;

        var result = await service.CreateJoinLinkAsync(UserId, new JoinLinkCreateDto { Channel = channel, InviteUrl = url });

        Assert.False(result.Status);
        Assert.Empty(context.PublicPageJoinLinks);
    }

    [Fact]
    public async Task CreateManual_ShouldBeUnverifiableAndRejectDuplicates()
    {
        var (service, context) = await BuildAsync(withGroup: false);
        await using var _ = context;

        var created = await service.CreateJoinLinkAsync(UserId, new JoinLinkCreateDto { Channel = "WhatsApp", InviteUrl = WhatsAppInvite, Label = "Meu grupo" });
        var duplicate = await service.CreateJoinLinkAsync(UserId, new JoinLinkCreateDto { Channel = "WhatsApp", InviteUrl = WhatsAppInvite });

        Assert.False(created.Data!.JoinLinks[0].IsVerifiable);
        Assert.Equal("Meu grupo", created.Data.JoinLinks[0].Label);
        Assert.False(duplicate.Status);
    }

    [Fact]
    public async Task Update_ShouldDeactivateReactivateAndOnlyTouchTheOwnersLink()
    {
        var (service, context) = await BuildAsync(withGroup: false);
        await using var _ = context;
        var created = await service.CreateJoinLinkAsync(UserId, new JoinLinkCreateDto { Channel = "WhatsApp", InviteUrl = WhatsAppInvite });
        var id = created.Data!.JoinLinks[0].Id;

        var off = await service.UpdateJoinLinkAsync(UserId, id, new JoinLinkUpdateDto { Label = "Meu grupo", IsActive = false });
        Assert.Equal(JoinLinkRules.StatusInactive, off.Data!.JoinLinks[0].Status);
        Assert.Null(await service.ResolveJoinTargetAsync(id));

        var on = await service.UpdateJoinLinkAsync(UserId, id, new JoinLinkUpdateDto { Label = "Meu grupo", IsActive = true });
        Assert.Equal(JoinLinkRules.StatusActive, on.Data!.JoinLinks[0].Status);
        Assert.NotNull(await service.ResolveJoinTargetAsync(id));

        var other = await service.UpdateJoinLinkAsync(999, id, new JoinLinkUpdateDto { Label = "invasor", IsActive = false });
        Assert.False(other.Status);
        var removed = await service.DeleteJoinLinkAsync(999, id);
        Assert.False(removed.Status);
    }

    [Fact]
    public async Task ResolveJoinTarget_ShouldNeverReturnAnUnvalidatedAddress()
    {
        var (service, context) = await BuildAsync(withGroup: false);
        await using var _ = context;
        context.PublicPageJoinLinks.Add(new PublicPageJoinLink
        {
            UserId = UserId, TenantId = TenantId, Channel = "WhatsApp", Label = "Adulterado",
            InviteUrl = "https://evil.com/phishing", Status = JoinLinkRules.StatusActive
        });
        await context.SaveChangesAsync();

        Assert.Null(await service.ResolveJoinTargetAsync(context.PublicPageJoinLinks.Single().Id));
        Assert.Null(await service.ResolveJoinTargetAsync(12345));
    }

    [Fact]
    public async Task Avatar_ShouldSaveByContentReplaceAndRemove()
    {
        var (service, context) = await BuildAsync(withGroup: false);
        await using var _ = context;
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4, 5, 6];

        var first = await service.SaveAvatarAsync(UserId, new MemoryStream(png), png.Length);
        Assert.True(first.Status, first.Descricao);
        var firstUrl = first.Data!.AvatarUrl!;
        var firstPath = Path.Combine(_uploadsRoot, AvatarImageRules.ToRelativeStoragePath(firstUrl)!);
        Assert.True(File.Exists(firstPath));

        var second = await service.SaveAvatarAsync(UserId, new MemoryStream(png), png.Length);
        Assert.NotEqual(firstUrl, second.Data!.AvatarUrl);
        Assert.False(File.Exists(firstPath));

        var removed = await service.RemoveAvatarAsync(UserId);
        Assert.Null(removed.Data!.AvatarUrl);
    }

    [Fact]
    public async Task Avatar_ShouldRejectSvgAndOversizedFiles()
    {
        var (service, context) = await BuildAsync(withGroup: false);
        await using var _ = context;
        var svg = System.Text.Encoding.UTF8.GetBytes("<svg><script>alert(1)</script></svg>");

        var rejectedSvg = await service.SaveAvatarAsync(UserId, new MemoryStream(svg), svg.Length);
        var rejectedSize = await service.SaveAvatarAsync(UserId, new MemoryStream(new byte[16]), AvatarImageRules.MaxBytes + 1);

        Assert.False(rejectedSvg.Status);
        Assert.False(rejectedSize.Status);
        Assert.False(Directory.Exists(_uploadsRoot) && Directory.EnumerateFiles(_uploadsRoot, "*", SearchOption.AllDirectories).Any());
    }
}
