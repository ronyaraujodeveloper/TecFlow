using Ofertio.SharedUi.Helpers;
using Xunit;

namespace Ofertio.Tests.Unit.SharedUi;

/// <summary>Modo de seleção única do MultiSelectDropdown (combo de cidade em Meus dados).</summary>
public sealed class MultiSelectSingleTests
{
    private static readonly IReadOnlyList<MultiSelectOption> Cities =
    [
        new("100", "São Paulo"),
        new("101", "São José dos Campos"),
        new("102", "Campinas")
    ];

    [Fact]
    public void SelectSingle_ShouldReplaceThePreviousChoice() =>
        Assert.Equal(["102"], MultiSelectRules.SelectSingle("102"));

    [Fact]
    public void SelectedLabel_ShouldShowTheChosenCityOrNothing()
    {
        Assert.Equal("Campinas", MultiSelectRules.SelectedLabel(Cities, ["102"]));
        Assert.Null(MultiSelectRules.SelectedLabel(Cities, []));
        Assert.Null(MultiSelectRules.SelectedLabel(Cities, ["999"]));
    }

    [Fact]
    public void Filter_ShouldFindCitiesWithoutAccents() =>
        Assert.Equal(2, MultiSelectRules.Filter(Cities, "sao").Count);
}
