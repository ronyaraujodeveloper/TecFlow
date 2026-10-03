using TecFlow.Business.Service.WhatsApp;
using TecFlow.Core.Entities;

namespace TecFlow.Tests.Unit.WhatsApp;

public class WhatsAppBroadcastRulesTests
{
    [Theory]
    [InlineData(5, 15)]
    [InlineData(15, 15)]
    [InlineData(30, 30)]
    [InlineData(500, 180)]
    public void ClampIntervalSeconds_ShouldKeepAntiBanWindow(int input, int expected)
    {
        Assert.Equal(expected, WhatsAppBroadcastRules.ClampIntervalSeconds(input));
    }

    [Fact]
    public void ApplyCommissionTag_ShouldReplacePlaceholder()
    {
        var text = WhatsAppBroadcastRules.ApplyCommissionTag(
            "Oferta [LINK_COMISSAO]",
            "https://shopee.com.br/x");

        Assert.Equal("Oferta https://shopee.com.br/x", text);
    }

    [Fact]
    public void ComposeDispatchMessage_ShouldAppendLinkAfterCopy()
    {
        var text = WhatsAppBroadcastRules.ComposeDispatchMessage(
            "Oferta relâmpago",
            "https://shopee.com.br/x");

        Assert.Equal("Oferta relâmpago\n\nhttps://shopee.com.br/x", text);
    }

    [Fact]
    public void SplitDispatchMessage_ShouldSeparateCopyAndLink()
    {
        var (copy, link) = WhatsAppBroadcastRules.SplitDispatchMessage(
            "Oferta relâmpago\n\nhttps://shopee.com.br/x");

        Assert.Equal("Oferta relâmpago", copy);
        Assert.Equal("https://shopee.com.br/x", link);
    }

    [Theory]
    [InlineData("admin", true)]
    [InlineData("superadmin", true)]
    [InlineData("super-admin", true)]
    [InlineData("member", false)]
    public void IsPrivilegedWhatsAppRole_ShouldAcceptAdminAndSuperAdmin(string role, bool expected)
    {
        Assert.Equal(expected, WhatsAppBroadcastRules.IsPrivilegedWhatsAppRole(role));
    }

    [Fact]
    public void PhoneOrJidMatchesOwner_ShouldMatchConnectedNumber()
    {
        Assert.True(WhatsAppBroadcastRules.PhoneOrJidMatchesOwner(
            "5511999887766@s.whatsapp.net",
            "11999887766"));
        Assert.False(WhatsAppBroadcastRules.PhoneOrJidMatchesOwner(
            "5511888777666@s.whatsapp.net",
            "11999887766"));
    }

    [Fact]
    public void ContainsHttpUrl_ShouldDetectPastedLinks()
    {
        Assert.True(WhatsAppBroadcastRules.ContainsHttpUrl("Veja https://shopee.com.br/x agora"));
        Assert.Equal("Veja agora", WhatsAppBroadcastRules.StripHttpUrls("Veja https://shopee.com.br/x agora"));
    }

    [Fact]
    public void SerializeJids_ShouldDeduplicate()
    {
        var json = WhatsAppBroadcastRules.SerializeJids(["  a@g.us ", "a@g.us", "b@g.us"]);
        var jids = WhatsAppBroadcastRules.DeserializeJids(json);

        Assert.Equal(2, jids.Count);
        Assert.Contains("a@g.us", jids);
        Assert.Contains("b@g.us", jids);
    }

    [Fact]
    public void ToUiStatus_ShouldMapCampaignStates()
    {
        Assert.Equal("Agendada", WhatsAppBroadcastRules.ToUiStatus(WhatsAppBroadcastStatuses.Pending));
        Assert.Equal("Enviando", WhatsAppBroadcastRules.ToUiStatus(WhatsAppBroadcastStatuses.Processing));
        Assert.Equal("Concluída", WhatsAppBroadcastRules.ToUiStatus(WhatsAppBroadcastStatuses.Completed));
        Assert.Equal("Falhou", WhatsAppBroadcastRules.ToUiStatus(WhatsAppBroadcastStatuses.Failed));
        Assert.Equal("Cancelado", WhatsAppBroadcastRules.ToUiStatus(WhatsAppBroadcastStatuses.Cancelled));
    }

    [Fact]
    public void ResolveEditScheduledAt_ShouldKeepFutureDate()
    {
        var now = new DateTime(2026, 10, 3, 19, 0, 0, DateTimeKind.Local);
        var scheduled = now.AddHours(2);

        var resolved = WhatsAppBroadcastRules.ResolveEditScheduledAt(scheduled, now, out var adjusted);

        Assert.False(adjusted);
        Assert.Equal(scheduled, resolved);
    }

    [Fact]
    public void ResolveEditScheduledAt_ShouldAdvancePastDateByTenMinutes()
    {
        var now = new DateTime(2026, 10, 3, 19, 0, 0, DateTimeKind.Local);
        var scheduled = now.AddMinutes(-30);

        var resolved = WhatsAppBroadcastRules.ResolveEditScheduledAt(scheduled, now, out var adjusted);

        Assert.True(adjusted);
        Assert.Equal(now.AddMinutes(10), resolved);
    }

    [Fact]
    public void JobCoordinator_ShouldCancelRegisteredCampaign()
    {
        var coordinator = new WhatsAppBroadcastJobCoordinator();
        using var parent = new CancellationTokenSource();
        var token = coordinator.Register(10, parent.Token);

        Assert.True(coordinator.Cancel(10));
        Assert.True(token.IsCancellationRequested);
        coordinator.Unregister(10);
        Assert.False(coordinator.Cancel(10));
    }
}
