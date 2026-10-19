using Ofertio.Business.Service.Groups;
using Ofertio.SharedUi.Models;

namespace Ofertio.Tests.Unit.Groups;

public class MonitoredGroupSyncRulesTests
{
    [Fact]
    public void BackgroundStartedMessage_ShouldTellUserWorkContinuesOffRequest()
    {
        Assert.Equal(
            "Sincronização iniciada! Os links estão sendo capturados em segundo plano.",
            MonitoredGroupSyncRules.BackgroundStartedMessage);
    }

    [Fact]
    public void ResolveHttpTimeout_ShouldBeAtLeastThreeMinutes()
    {
        Assert.Equal(TimeSpan.FromMinutes(3), MonitoredGroupSyncRules.ResolveHttpTimeout(30));
        Assert.Equal(TimeSpan.FromSeconds(240), MonitoredGroupSyncRules.ResolveHttpTimeout(240));
    }

    [Fact]
    public void FiltersResetMessage_ShouldConfirmFiltersWereReset()
    {
        Assert.Equal("Filtros redefinidos com sucesso.", MonitoredGroupSyncRules.FiltersResetMessage);
    }

    [Fact]
    public void BuildCompletionMessage_ShouldReportNewGroupsAndMessages()
    {
        Assert.Equal(
            "Sincronização concluída! 2 novos grupos e 15 novas mensagens atualizadas.",
            MonitoredGroupSyncRules.BuildCompletionMessage(groupsBefore: 3, groupsAfter: 5, offersBefore: 10, offersAfter: 25));
    }

    [Fact]
    public void BuildCompletionMessage_ShouldUseSingularForOne()
    {
        Assert.Equal(
            "Sincronização concluída! 1 novo grupo e 1 nova mensagem atualizada.",
            MonitoredGroupSyncRules.BuildCompletionMessage(groupsBefore: 0, groupsAfter: 1, offersBefore: 4, offersAfter: 5));
    }

    [Fact]
    public void BuildCompletionMessage_ShouldNeverReportNegativeCounts()
    {
        Assert.Equal(
            "Sincronização concluída! 0 novos grupos e 0 novas mensagens atualizadas.",
            MonitoredGroupSyncRules.BuildCompletionMessage(groupsBefore: 5, groupsAfter: 4, offersBefore: 30, offersAfter: 12));
    }

    [Fact]
    public void IsStatusFromCurrentRun_ShouldIgnoreStatusFromPreviousSync()
    {
        var startedAt = new DateTime(2026, 10, 18, 21, 0, 0, DateTimeKind.Utc);

        Assert.True(MonitoredGroupSyncRules.IsStatusFromCurrentRun(startedAt.AddSeconds(40), startedAt));
        Assert.True(MonitoredGroupSyncRules.IsStatusFromCurrentRun(startedAt.AddSeconds(-2), startedAt));
        Assert.False(MonitoredGroupSyncRules.IsStatusFromCurrentRun(startedAt.AddMinutes(-10), startedAt));
        Assert.False(MonitoredGroupSyncRules.IsStatusFromCurrentRun(null, startedAt));
    }

    [Fact]
    public void MonitoredOfferFilters_Default_ShouldHaveNoActiveFilters()
    {
        var defaults = MonitoredOfferFilters.Default(GroupOfferCaptureRules.OffersPageSize);

        Assert.Equal(MonitoredOfferFilters.DefaultHours, defaults.Hours);
        Assert.Null(defaults.MinPrice);
        Assert.Null(defaults.MaxPrice);
        Assert.Null(defaults.Store);
        Assert.Equal(0, defaults.ActiveCount(GroupOfferCaptureRules.OffersPageSize));
    }
}
