using System.ComponentModel.DataAnnotations;
using Ofertio.Business.Dto;
using Ofertio.Business.Service.Common;

namespace Ofertio.Tests.Unit.Common;

public class MoneyAmountRulesTests
{
    public static IEnumerable<object[]> Samples() =>
        MoneyAmountRules.Samples.Select(sample => new object[] { sample });

    [Theory]
    [MemberData(nameof(Samples))]
    public void TryParse_MatchesTheSharedMatrix(MoneyAmountSample sample)
    {
        var parsed = MoneyAmountRules.TryParse(sample.Text, out var amount);

        Assert.Equal(sample.IsValid, parsed);
        if (sample.IsValid)
        {
            Assert.Equal(sample.Expected, amount);
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-0.01)]
    [InlineData(1000000.01)]
    public void IsAllowed_RejectsNegativeAndAboveMax(decimal amount)
    {
        Assert.False(MoneyAmountRules.IsAllowed(amount));
    }

    [Fact]
    public void IsAllowed_AcceptsZeroUpToMax()
    {
        Assert.True(MoneyAmountRules.IsAllowed(0m));
        Assert.True(MoneyAmountRules.IsAllowed(89.90m));
        Assert.True(MoneyAmountRules.IsAllowed(1_000_000m));
    }

    [Fact]
    public void NewCampaignDto_KeepsNameLimitsAndRejectsInvalidBudget()
    {
        Assert.False(IsValid(new NewCampaignDto { Name = "", Description = "texto", Budget = 10m }));
        Assert.False(IsValid(new NewCampaignDto { Name = new string('a', 101), Description = "texto", Budget = 10m }));
        Assert.False(IsValid(new NewCampaignDto { Name = "Campanha", Description = "texto", Budget = -1m }));
        Assert.False(IsValid(new NewCampaignDto { Name = "Campanha", Description = "texto", Budget = 1_000_000.01m }));
        Assert.True(IsValid(new NewCampaignDto { Name = "Campanha", Description = "texto", Budget = 89.90m }));
        Assert.True(IsValid(new NewCampaignDto { Name = "Campanha", Description = "texto", Budget = 0m }));
    }

    private static bool IsValid(NewCampaignDto dto)
    {
        var results = new List<ValidationResult>();
        return Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);
    }
}
