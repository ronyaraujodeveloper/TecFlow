using Ofertio.Business.Service.Mail;
using Ofertio.Business.Service.Master;
using Xunit;

namespace Ofertio.Tests.Unit.Billing;

public sealed class TrialRulesTests
{
    private static readonly DateTime Now = new(2026, 11, 15, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(0, TrialRules.DefaultDays)]
    [InlineData(-5, TrialRules.DefaultDays)]
    [InlineData(30, 30)]
    [InlineData(9999, TrialRules.MaxDays)]
    public void NormalizeDays_ShouldClampToValidRange(int input, int expected) =>
        Assert.Equal(expected, TrialRules.NormalizeDays(input));

    [Fact]
    public void EndsAt_ShouldAddConfiguredDays() =>
        Assert.Equal(Now.AddDays(21), TrialRules.EndsAt(Now, 21));

    [Fact]
    public void IsTrialPlan_ShouldIgnoreCaseAndSpaces()
    {
        Assert.True(TrialRules.IsTrialPlan(" trial "));
        Assert.False(TrialRules.IsTrialPlan("Starter"));
        Assert.False(TrialRules.IsTrialPlan(null));
    }

    [Theory]
    [InlineData(null, "Starter")]
    [InlineData("", "Starter")]
    [InlineData("Trial", "Starter")]
    [InlineData("Pro", "Pro")]
    public void ResolvePostTrialPlan_ShouldFallBackToStarter(string? configured, string expected) =>
        Assert.Equal(expected, TrialRules.ResolvePostTrialPlan(configured));

    [Fact]
    public void ShouldSendEndingNotice_OnlyInsideWindowAndOnce()
    {
        Assert.True(TrialRules.ShouldSendEndingNotice(TenantStatuses.Trial, Now.AddDays(2), null, Now));
        Assert.False(TrialRules.ShouldSendEndingNotice(TenantStatuses.Trial, Now.AddDays(10), null, Now));
        Assert.False(TrialRules.ShouldSendEndingNotice(TenantStatuses.Trial, Now.AddDays(2), Now.AddHours(-1), Now));
        Assert.False(TrialRules.ShouldSendEndingNotice(TenantStatuses.Trial, Now.AddDays(-1), null, Now));
        Assert.False(TrialRules.ShouldSendEndingNotice(TenantStatuses.Ativo, Now.AddDays(2), null, Now));
    }

    [Fact]
    public void ShouldConvertToPaid_RequiresEndedTrialAndSubscription()
    {
        Assert.True(TrialRules.ShouldConvertToPaid(TenantStatuses.Trial, Now.AddMinutes(-1), "sub_1", Now));
        Assert.False(TrialRules.ShouldConvertToPaid(TenantStatuses.Trial, Now.AddMinutes(-1), null, Now));
        Assert.False(TrialRules.ShouldConvertToPaid(TenantStatuses.Trial, Now.AddDays(1), "sub_1", Now));
        Assert.False(TrialRules.ShouldConvertToPaid(TenantStatuses.Ativo, Now.AddMinutes(-1), "sub_1", Now));
    }

    [Fact]
    public void TrialEndingEmail_WithCard_MentionsChargeAndPlan()
    {
        var mail = TransactionalEmailRules.TrialEnding("a@b.com", true, "Starter", 49.9m, Now.AddDays(2), "https://x/meu-plano");

        Assert.Contains("cobrado", mail.TextBody);
        Assert.Contains("Starter", mail.TextBody);
        Assert.Contains("https://x/meu-plano", mail.HtmlBody);
    }

    [Fact]
    public void TrialEndingEmail_WithoutCard_AsksToChoosePlan()
    {
        var mail = TransactionalEmailRules.TrialEnding("a@b.com", false, "Starter", 49.9m, Now.AddDays(2), "https://x/meu-plano");

        Assert.Contains("Escolha um plano", mail.TextBody);
        Assert.DoesNotContain("cobrado", mail.TextBody);
    }
}