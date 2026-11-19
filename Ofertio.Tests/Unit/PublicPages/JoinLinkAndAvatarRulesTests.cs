using Ofertio.Business.Service.PublicPages;
using Xunit;

namespace Ofertio.Tests.Unit.PublicPages;

public sealed class JoinLinkRulesTests
{
    [Theory]
    [InlineData("WhatsApp", "https://chat.whatsapp.com/Ab12Cd34Ef56Gh78Ij90Kl", "https://chat.whatsapp.com/Ab12Cd34Ef56Gh78Ij90Kl")]
    [InlineData("whatsapp", "chat.whatsapp.com/Ab12Cd34Ef56Gh78Ij90Kl/", "https://chat.whatsapp.com/Ab12Cd34Ef56Gh78Ij90Kl")]
    [InlineData("WhatsApp", "https://www.whatsapp.com/channel/0029VaAbCdEf12", "https://whatsapp.com/channel/0029VaAbCdEf12")]
    [InlineData("Telegram", "https://t.me/+AbCdEfGhIjKl12", "https://t.me/+AbCdEfGhIjKl12")]
    [InlineData("Telegram", "https://t.me/joinchat/AbCdEfGhIjKl", "https://t.me/joinchat/AbCdEfGhIjKl")]
    [InlineData("Telegram", "t.me/achadinhos_vip", "https://t.me/achadinhos_vip")]
    public void TryNormalizeInviteUrl_ShouldAcceptValidInvites(string channel, string url, string expected)
    {
        Assert.True(JoinLinkRules.TryNormalizeInviteUrl(channel, url, out var normalized, out var error));
        Assert.Equal(expected, normalized);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("WhatsApp", "https://evil.com/Ab12Cd34Ef56Gh78Ij90Kl")]
    [InlineData("WhatsApp", "https://chat.whatsapp.com.evil.com/Ab12Cd34Ef56Gh78Ij90Kl")]
    [InlineData("WhatsApp", "http://chat.whatsapp.com/Ab12Cd34Ef56Gh78Ij90Kl")]
    [InlineData("WhatsApp", "https://wa.me/5511999999999")]
    [InlineData("WhatsApp", "https://chat.whatsapp.com/curto")]
    [InlineData("WhatsApp", "https://user:pass@chat.whatsapp.com/Ab12Cd34Ef56Gh78Ij90Kl")]
    [InlineData("WhatsApp", "https://chat.whatsapp.com/Ab12Cd34Ef56Gh78Ij90Kl?x=1")]
    [InlineData("Telegram", "https://t.me/ab")]
    [InlineData("Telegram", "https://t.me.evil.com/+AbCdEfGhIjKl12")]
    [InlineData("Telegram", "javascript:alert(1)")]
    [InlineData("Telegram", "")]
    [InlineData("Telegram", null)]
    [InlineData("Discord", "https://discord.gg/abc12345")]
    [InlineData(null, "https://t.me/achadinhos_vip")]
    public void TryNormalizeInviteUrl_ShouldRejectAnythingElse(string? channel, string? url)
    {
        Assert.False(JoinLinkRules.TryNormalizeInviteUrl(channel, url, out var normalized, out var error));
        Assert.Equal(string.Empty, normalized);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void TryNormalizeInviteUrl_ShouldNotAcceptTheOtherChannelsDomain() =>
        Assert.False(JoinLinkRules.TryNormalizeInviteUrl("WhatsApp", "https://t.me/+AbCdEfGhIjKl12", out _, out _));

    [Theory]
    [InlineData(0, 100, false)]
    [InlineData(3, 2, false)]
    [InlineData(3, 3, true)]
    [InlineData(3, 4, true)]
    public void ExceedsLimit_ShouldTreatZeroAsUnlimited(int limit, int count, bool expected) =>
        Assert.Equal(expected, JoinLinkRules.ExceedsLimit(limit, count));

    [Fact]
    public void AfterFailedCheck_ShouldOnlyDeactivateOnTheSecondConsecutiveFailure()
    {
        var first = JoinLinkRules.AfterFailedCheck(0);
        var second = JoinLinkRules.AfterFailedCheck(first.FailedChecks);

        Assert.Equal(JoinLinkRules.StatusActive, first.Status);
        Assert.Null(first.Reason);
        Assert.Equal(JoinLinkRules.StatusInactive, second.Status);
        Assert.NotNull(second.Reason);
    }

    [Theory]
    [InlineData(true, 1250, "1,3 mil membros")]
    [InlineData(true, 87, "87 membros")]
    [InlineData(true, 2_500_000, "2,5 mi membros")]
    [InlineData(false, 1250, "")]
    [InlineData(true, null, "")]
    [InlineData(true, 0, "")]
    public void MemberText_ShouldShowCountOnlyWhenChosenAndKnown(bool show, int? count, string expected) =>
        Assert.Equal(expected, JoinLinkRules.MemberText(show, count));

    [Fact]
    public void NormalizeLabel_ShouldTrimCollapseSpacesAndFallBack()
    {
        Assert.Equal("Achadinhos VIP", JoinLinkRules.NormalizeLabel("  Achadinhos    VIP ", "x"));
        Assert.Equal("Grupo", JoinLinkRules.NormalizeLabel("   ", "Grupo"));
        Assert.Equal(JoinLinkRules.MaxLabelLength, JoinLinkRules.NormalizeLabel(new string('a', 200), "x").Length);
    }

    [Theory]
    [InlineData("Active", true)]
    [InlineData("Unverified", true)]
    [InlineData("Inactive", false)]
    [InlineData(null, false)]
    public void IsPublic_ShouldHideInactiveLinks(string? status, bool expected) =>
        Assert.Equal(expected, JoinLinkRules.IsPublic(status));
}

public sealed class AvatarImageRulesTests
{
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0];
    private static readonly byte[] Webp = [(byte)'R', (byte)'I', (byte)'F', (byte)'F', 0, 0, 0, 0, (byte)'W', (byte)'E', (byte)'B', (byte)'P'];
    private static readonly byte[] Svg = System.Text.Encoding.UTF8.GetBytes("<svg xmlns='http://www.w3.org/2000/svg'><script>alert(1)</script></svg>");

    [Fact]
    public void DetectExtension_ShouldRecognizeByContent()
    {
        Assert.Equal(".jpg", AvatarImageRules.DetectExtension(Jpeg));
        Assert.Equal(".png", AvatarImageRules.DetectExtension(Png));
        Assert.Equal(".webp", AvatarImageRules.DetectExtension(Webp));
    }

    [Fact]
    public void DetectExtension_ShouldRejectSvgAndRandomBytes()
    {
        Assert.Null(AvatarImageRules.DetectExtension(Svg));
        Assert.Null(AvatarImageRules.DetectExtension([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12]));
        Assert.Null(AvatarImageRules.DetectExtension([]));
    }

    [Fact]
    public void Validate_ShouldCheckSizeAndType()
    {
        Assert.Null(AvatarImageRules.Validate(1000, Jpeg));
        Assert.Equal(AvatarImageRules.TooLargeMessage, AvatarImageRules.Validate(AvatarImageRules.MaxBytes + 1, Jpeg));
        Assert.Equal(AvatarImageRules.InvalidTypeMessage, AvatarImageRules.Validate(1000, Svg));
        Assert.Equal(AvatarImageRules.InvalidTypeMessage, AvatarImageRules.Validate(0, Jpeg));
    }

    [Fact]
    public void BuildFileName_ShouldBeRandomAndKeepOnlyTheDetectedExtension()
    {
        var first = AvatarImageRules.BuildFileName(".png");
        var second = AvatarImageRules.BuildFileName(".png");

        Assert.EndsWith(".png", first);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void ToRelativeStoragePath_ShouldOnlyAcceptOurAvatarUrls()
    {
        var tenant = Guid.NewGuid();
        var url = AvatarImageRules.BuildPublicUrl(tenant, "abc.jpg");

        Assert.NotNull(AvatarImageRules.ToRelativeStoragePath(url));
        Assert.Null(AvatarImageRules.ToRelativeStoragePath("/uploads/products/1/a.jpg"));
        Assert.Null(AvatarImageRules.ToRelativeStoragePath("/uploads/avatars/../../web.config"));
        Assert.Null(AvatarImageRules.ToRelativeStoragePath("https://evil.com/x.jpg"));
        Assert.Null(AvatarImageRules.ToRelativeStoragePath(null));
    }
}
