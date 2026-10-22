using Ofertio.Business.Dto;
using Ofertio.Business.Service.Radar;
using Ofertio.Core.Enums;
using Ofertio.Database.Filter;
using Ofertio.SharedUi.Helpers;

namespace Ofertio.Tests.Unit.SharedUi;

public class OfferSearchFilterOptionsTests
{
    private static readonly IReadOnlyList<MultiSelectOption> Options =
    [
        new("MLB-NOTEBOOKS", "Notebooks"),
        new("MLB-ELETRONICOS", "Eletrônicos"),
        new("MLB-TABLETS", "Tablets")
    ];

    [Theory]
    [InlineData(null, 3)]
    [InlineData("no", 3)]
    [InlineData("  ta ", 3)]
    [InlineData("note", 1)]
    [InlineData("ELETRO", 1)]
    [InlineData("eletrôn", 1)]
    [InlineData("xyz", 0)]
    public void Filter_ShouldOnlyFilterFromThreeCharactersIgnoringCaseAndAccents(string? term, int expected)
    {
        Assert.Equal(expected, MultiSelectRules.Filter(Options, term).Count);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(3, false)]
    [InlineData(6, false)]
    [InlineData(7, true)]
    public void ShowSearch_ShouldOnlyShowInnerSearchWithMoreThanSixOptions(int count, bool expected)
    {
        Assert.Equal(expected, MultiSelectRules.ShowSearch(count));
        Assert.False(MultiSelectRules.ShowSearch(OfferSearchFilterOptions.Platforms.Count));
        Assert.False(MultiSelectRules.ShowSearch(OfferSearchFilterOptions.Qualifiers.Count));
    }

    [Theory]
    [InlineData(-1, 5, 1, 0)]
    [InlineData(0, 5, 1, 1)]
    [InlineData(4, 5, 1, 4)]
    [InlineData(2, 5, -1, 1)]
    [InlineData(0, 5, -1, 0)]
    [InlineData(-1, 0, 1, -1)]
    public void MoveActive_ShouldClampKeyboardNavigationToTheVisibleOptions(int current, int count, int step, int expected)
    {
        Assert.Equal(expected, MultiSelectRules.MoveActive(current, count, step));
    }

    [Fact]
    public void IsSearchTooShort_ShouldFlagOneOrTwoTypedCharacters()
    {
        Assert.False(MultiSelectRules.IsSearchTooShort(null));
        Assert.False(MultiSelectRules.IsSearchTooShort("  "));
        Assert.True(MultiSelectRules.IsSearchTooShort("ta"));
        Assert.False(MultiSelectRules.IsSearchTooShort("tab"));
    }

    [Fact]
    public void Summary_ShouldShowSelectedCountOnlyWhenSomethingIsSelected()
    {
        Assert.Equal("Categorias", MultiSelectRules.Summary("Categorias", 0));
        Assert.Equal("Filtros e Qualificadores (2)", MultiSelectRules.Summary("Filtros e Qualificadores", 2));
    }

    [Fact]
    public void ToggleSelectAllAndClear_ShouldRespectVisibleOptions()
    {
        IReadOnlyCollection<string> selected = ["MLB-TABLETS"];

        var toggled = MultiSelectRules.Toggle(selected, "MLB-NOTEBOOKS", true);
        var untoggled = MultiSelectRules.Toggle(toggled, "mlb-tablets", false);
        var all = MultiSelectRules.SelectAll(selected, MultiSelectRules.Filter(Options, "note"));
        var cleared = MultiSelectRules.ClearVisible(["MLB-TABLETS", "MLB-NOTEBOOKS"], MultiSelectRules.Filter(Options, "note"));

        Assert.Equal(["MLB-TABLETS", "MLB-NOTEBOOKS"], toggled);
        Assert.Equal(["MLB-NOTEBOOKS"], untoggled);
        Assert.Equal(["MLB-TABLETS", "MLB-NOTEBOOKS"], all);
        Assert.Equal(["MLB-TABLETS"], cleared);
    }

    [Fact]
    public void ApplySelections_ShouldMapPlatformsQualifiersAndCategoriesToTheFilter()
    {
        var filter = OfferSearchFilterOptions.ApplySelections(
            new OfficialCatalogSearchFilter(),
            [OfferSearchFilterOptions.MercadoLivre, OfferSearchFilterOptions.Amazon],
            [OfferSearchFilterOptions.FreeShippingOnly, OfferSearchFilterOptions.MainProductsOnly, OfferSearchFilterOptions.TrustedSellersOnly],
            ["MLB-NOTEBOOKS"]);

        Assert.True(filter.MercadoLivre);
        Assert.False(filter.Shopee);
        Assert.True(filter.Amazon);
        Assert.True(filter.FreeShippingOnly);
        Assert.True(filter.MainProductsOnly);
        Assert.True(filter.TrustedSellersOnly);
        Assert.Null(filter.HasCoupon);
        Assert.Null(filter.OfficialStoreOnly);
        Assert.Null(filter.BestSellersOnly);
        Assert.Null(filter.MercadoLivreCategory);
        Assert.Equal(["MLB-NOTEBOOKS"], filter.MercadoLivreCategories!);
    }

    [Fact]
    public void ApplySelections_WithoutCategories_ShouldLeaveCategoryFilterEmpty()
    {
        var filter = OfferSearchFilterOptions.ApplySelections(
            new OfficialCatalogSearchFilter(),
            OfferSearchFilterOptions.DefaultPlatforms,
            [],
            []);

        Assert.True(filter.MercadoLivre && filter.Shopee && filter.Amazon);
        Assert.Null(filter.MercadoLivreCategories);
    }

    [Fact]
    public void Categories_ShouldCountLoadedOffersAndKeepKnownSelections()
    {
        OfficialCatalogCategoryDto[] categories =
        [
            new() { Id = "MLB-NOTEBOOKS", Name = "Notebooks" },
            new() { Id = "MLB-TABLETS", Name = "" }
        ];
        OfficialCatalogProductDto[] loaded =
        [
            new() { PlatformType = MarketplaceType.MercadoLivre, DomainId = "MLB-NOTEBOOKS" },
            new() { PlatformType = MarketplaceType.MercadoLivre, DomainId = "MLB-NOTEBOOKS" }
        ];

        var options = OfferSearchFilterOptions.Categories(categories, loaded);
        var kept = OfferSearchFilterOptions.KeepKnownCategories(["mlb-notebooks", "MLB-CHINELOS"], categories);

        Assert.Equal([2, 0], options.Select(option => option.Count ?? -1));
        Assert.Equal("MLB-TABLETS", options[1].Label);
        Assert.Equal(["mlb-notebooks"], kept);
    }

    [Fact]
    public void SelectedWithoutOffers_ShouldListOnlyMarkedCategoriesWithZeroLoadedOffers()
    {
        MultiSelectOption[] options =
        [
            new("MLB-FLIP_FLOPS", "Chinelos", 8),
            new("MLB-SANDALS_AND_CLOGS", "Sandálias e tamancos", 0),
            new("MLB-BOOTS", "Botas", 0)
        ];

        var empty = OfferSearchFilterOptions.SelectedWithoutOffers(["MLB-FLIP_FLOPS", "mlb-sandals_and_clogs"], options);

        Assert.Equal(["Sandálias e tamancos"], empty.Select(option => option.Label));
    }

    [Fact]
    public void ApplySelections_ShouldSendSelectedCategoriesInTheQueryStringAsRepeatedKeys()
    {
        var filter = OfferSearchFilterOptions.ApplySelections(
            new OfficialCatalogSearchFilter { Keyword = "havaianas" },
            OfferSearchFilterOptions.DefaultPlatforms,
            [],
            ["MLB-FLIP_FLOPS", "MLB-SANDALS_AND_CLOGS"]);

        var query = Ofertio.SharedUi.Extensions.FilterQueryStringExtensions.ToQueryString(filter);

        Assert.Contains("MercadoLivreCategories=MLB-FLIP_FLOPS&MercadoLivreCategories=MLB-SANDALS_AND_CLOGS", query);
        Assert.DoesNotContain("System.Collections", query);
        Assert.Contains("Keyword=havaianas", query);
    }

    [Fact]
    public void AdvancedFilters_ShouldCountActiveFieldsValidateRangeAndCopyIndependently()
    {
        var filters = new OfferSearchAdvancedFilters();
        Assert.Equal(0, filters.ActiveCount);
        Assert.Null(filters.Validate());

        var draft = filters.Copy();
        draft.MinPrice = 500m;
        draft.MaxPrice = 100m;
        draft.SortBy = OfficialCatalogSearchRules.SortLowestPrice;

        Assert.Equal(3, draft.ActiveCount);
        Assert.NotNull(draft.Validate());
        Assert.Null(filters.MinPrice);
        Assert.Equal(OfficialCatalogSearchRules.SortRelevance, filters.SortBy);

        draft.MaxPrice = 900m;
        Assert.Null(draft.Validate());
    }
}
