using TecFlow.Business.Dto;
using TecFlow.Business.Service.Groups;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;

namespace TecFlow.Tests.Unit.Groups;

public class OfferFeedVisibilityRulesTests
{
    [Theory]
    [InlineData(true, GroupOfferStatuses.Ativo, true)]
    [InlineData(true, GroupOfferStatuses.PrecoAlterado, true)]
    [InlineData(true, GroupOfferStatuses.PrecoSuspeito, true)]
    [InlineData(true, GroupOfferStatuses.Verificando, true)]
    [InlineData(true, GroupOfferStatuses.Esgotado, false)]
    [InlineData(true, GroupOfferStatuses.Inativo, false)]
    [InlineData(true, GroupOfferStatuses.VitrineGenerica, false)]
    [InlineData(true, "StatusDesconhecido", false)]
    [InlineData(false, GroupOfferStatuses.Ativo, false)]
    public void IsVisibleInFeed_ShouldOnlyKeepAvailableProductsWithActiveStatus(bool isAvailable, string status, bool expected)
    {
        Assert.Equal(expected, OfferFeedVisibilityRules.IsVisibleInFeed(OfferType.Product, isAvailable, status));
    }

    [Fact]
    public void IsVisibleInFeed_ShouldKeepAvailableCouponsRegardlessOfStatus()
    {
        Assert.True(OfferFeedVisibilityRules.IsVisibleInFeed(OfferType.Coupon, true, GroupOfferStatuses.Esgotado));
        Assert.False(OfferFeedVisibilityRules.IsVisibleInFeed(OfferType.Coupon, false, GroupOfferStatuses.Ativo));
    }

    [Fact]
    public void RemoveFromFeed_ShouldDropOnlyTheVerifiedOffer()
    {
        var offers = new List<GroupCapturedOfferDto> { new() { Id = 1 }, new() { Id = 2 }, new() { Id = 3 } };

        Assert.True(OfferFeedVisibilityRules.RemoveFromFeed(offers, 2));
        Assert.False(OfferFeedVisibilityRules.RemoveFromFeed(offers, 2));
        Assert.Equal([1, 3], offers.Select(item => item.Id));
    }

    [Fact]
    public void ReplaceInFeed_ShouldKeepPosition()
    {
        var offers = new List<GroupCapturedOfferDto> { new() { Id = 1 }, new() { Id = 2, ProductName = "Antigo" }, new() { Id = 3 } };

        Assert.True(OfferFeedVisibilityRules.ReplaceInFeed(offers, new GroupCapturedOfferDto { Id = 2, ProductName = "Novo" }));
        Assert.Equal("Novo", offers[1].ProductName);
        Assert.False(OfferFeedVisibilityRules.ReplaceInFeed(offers, new GroupCapturedOfferDto { Id = 9 }));
    }
}
