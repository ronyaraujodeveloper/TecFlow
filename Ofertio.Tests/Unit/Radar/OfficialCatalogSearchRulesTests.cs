using Ofertio.Business.Dto;
using Ofertio.Business.Service.Radar;
using Ofertio.Core.Enums;
using Ofertio.Database.Filter;

namespace Ofertio.Tests.Unit.Radar;

public class OfficialCatalogSearchRulesTests
{
    [Fact]
    public void BuildShorterQueries_ShouldDropLastWordsProgressively()
    {
        Assert.Equal(
            ["Dell Inspiron i7", "Dell Inspiron", "Dell"],
            OfficialCatalogSearchRules.BuildShorterQueries("  Dell  Inspiron i7 16GB "));
        Assert.Empty(OfficialCatalogSearchRules.BuildShorterQueries("notebook"));
        Assert.Empty(OfficialCatalogSearchRules.BuildShorterQueries("a b"));
    }

    [Fact]
    public void ApplySort_ShouldOrderByLowestPriceOrBiggestDiscountAndKeepRelevance()
    {
        IReadOnlyList<OfficialCatalogProductDto> items =
        [
            new() { ProductId = "A", Price = 300m, OriginalPrice = 330m },
            new() { ProductId = "B", Price = 100m, OriginalPrice = 200m },
            new() { ProductId = "C", Price = null },
            new() { ProductId = "D", Price = 200m }
        ];

        Assert.Equal(["B", "D", "A", "C"], OfficialCatalogSearchRules.ApplySort(items, "menor_preco").Select(item => item.ProductId));
        Assert.Equal(["B", "A", "D", "C"], OfficialCatalogSearchRules.ApplySort(items, "maior_desconto").Select(item => item.ProductId));
        Assert.Equal(["A", "B", "C", "D"], OfficialCatalogSearchRules.ApplySort(items, "relevancia").Select(item => item.ProductId));
        Assert.Equal(["A", "B", "C", "D"], OfficialCatalogSearchRules.ApplySort(items, null).Select(item => item.ProductId));
    }

    [Fact]
    public void PassesOptionalFilters_FreeShippingOnly_ShouldRequireFreeShipping()
    {
        var filter = new OfficialCatalogSearchFilter { FreeShippingOnly = true };

        Assert.True(OfficialCatalogSearchRules.PassesOptionalFilters(new OfficialCatalogProductDto { Shipping = "Frete grátis" }, filter));
        Assert.False(OfficialCatalogSearchRules.PassesOptionalFilters(new OfficialCatalogProductDto { Shipping = "R$ 19,90" }, filter));
        Assert.False(OfficialCatalogSearchRules.PassesOptionalFilters(new OfficialCatalogProductDto(), filter));
        Assert.True(OfficialCatalogSearchRules.PassesOptionalFilters(new OfficialCatalogProductDto(), new OfficialCatalogSearchFilter()));
    }

    [Fact]
    public void FilterLoadedResults_ShouldFilterOnlyLoadedItemsByStoreCategoryPriceAndShipping()
    {
        IReadOnlyList<OfficialCatalogProductDto> items =
        [
            new() { ProductId = "ML-NB", PlatformType = MarketplaceType.MercadoLivre, DomainId = "MLB-NOTEBOOKS", Price = 4000m, Shipping = "Frete grátis" },
            new() { ProductId = "ML-PC", PlatformType = MarketplaceType.MercadoLivre, DomainId = "MLB-DESKTOP_COMPUTERS", Price = 3000m, Shipping = "Frete grátis" },
            new() { ProductId = "SHP", PlatformType = MarketplaceType.Shopee, Price = 3500m },
            new() { ProductId = "LOCAL", PlatformType = null, Price = 100m }
        ];

        Assert.Equal(
            ["ML-NB", "SHP", "LOCAL"],
            OfficialCatalogSearchRules.FilterLoadedResults(items, new OfficialCatalogSearchFilter
            {
                MercadoLivre = true,
                Shopee = true,
                MercadoLivreCategory = "MLB-NOTEBOOKS"
            }).Select(item => item.ProductId));
        Assert.Equal(
            ["ML-NB", "ML-PC"],
            OfficialCatalogSearchRules.FilterLoadedResults(items, new OfficialCatalogSearchFilter
            {
                MercadoLivre = true,
                Shopee = true,
                FreeShippingOnly = true
            }).Select(item => item.ProductId));
        Assert.Equal(
            ["ML-PC", "SHP"],
            OfficialCatalogSearchRules.FilterLoadedResults(items, new OfficialCatalogSearchFilter
            {
                MercadoLivre = true,
                Shopee = true,
                MinPrice = 2000m,
                MaxPrice = 3600m
            }).Select(item => item.ProductId));
        Assert.Equal(
            ["SHP", "LOCAL"],
            OfficialCatalogSearchRules.FilterLoadedResults(items, new OfficialCatalogSearchFilter
            {
                MercadoLivre = false,
                Shopee = true
            }).Select(item => item.ProductId));
    }

    [Fact]
    public void ApplyCommission_ShouldUseUserRateForMercadoLivreAndKeepShopeeApiRate()
    {
        var ml = new OfficialCatalogProductDto { PlatformType = MarketplaceType.MercadoLivre, Price = 4000m };
        var shopee = new OfficialCatalogProductDto { PlatformType = MarketplaceType.Shopee, Price = 100m, CommissionPercent = 8m, CommissionSource = "API Shopee" };
        var amazonWithoutRate = new OfficialCatalogProductDto { PlatformType = MarketplaceType.Amazon, Price = 500m };

        OfficialCatalogSearchRules.ApplyCommission(ml, 4.5m, null);
        OfficialCatalogSearchRules.ApplyCommission(shopee, 4.5m, null);
        OfficialCatalogSearchRules.ApplyCommission(amazonWithoutRate, 4.5m, null);

        Assert.Equal(4.5m, ml.CommissionPercent);
        Assert.Equal(180m, ml.EstimatedCommission);
        Assert.Equal("Sua taxa", ml.CommissionSource);
        Assert.Equal(8m, shopee.CommissionPercent);
        Assert.Equal(8m, shopee.EstimatedCommission);
        Assert.Equal("API Shopee", shopee.CommissionSource);
        Assert.Null(amazonWithoutRate.CommissionPercent);
        Assert.Null(amazonWithoutRate.EstimatedCommission);
    }

    [Fact]
    public void NormalizeCommissionPercent_ShouldRejectEmptyZeroAndAbsurdRates()
    {
        Assert.Null(OfficialCatalogSearchRules.NormalizeCommissionPercent(null));
        Assert.Null(OfficialCatalogSearchRules.NormalizeCommissionPercent(0m));
        Assert.Null(OfficialCatalogSearchRules.NormalizeCommissionPercent(95m));
        Assert.Equal(4.57m, OfficialCatalogSearchRules.NormalizeCommissionPercent(4.567m));
    }

    [Fact]
    public void ScoreOffer_ShouldRewardTrustedSellersAndFlagRiskyOffers()
    {
        var trusted = new OfficialCatalogProductDto
        {
            Price = 3000m,
            OriginalPrice = 4000m,
            Shipping = "Frete grátis",
            IsOfficialStore = true,
            SellerReputationLevel = "5_green",
            SellerPowerStatus = "platinum",
            SellerPositiveRate = 0.98m,
            IsFulfillment = true,
            Warranty = "Garantia de fábrica: 12 meses",
            Condition = "new",
            CommissionPercent = 5m,
            RatingAverage = 4.8m,
            RatingCount = 300,
            BestSellerPosition = 2,
            SoldQuantity = 1500
        };
        var risky = new OfficialCatalogProductDto
        {
            Price = 3000m,
            SellerReputationLevel = "1_red",
            SellerPositiveRate = 0.8m,
            Condition = "used"
        };

        OfficialCatalogSearchRules.ScoreOffer(trusted);
        OfficialCatalogSearchRules.ScoreOffer(risky);

        Assert.Equal(100, trusted.SafetyScore);
        Assert.Equal(90, trusted.AdvantageScore);
        Assert.Equal(95, trusted.OpportunityScore);
        Assert.Contains("-25%", trusted.Highlights);
        Assert.Contains("Loja oficial", trusted.Highlights);
        Assert.Contains("MercadoLíder Platinum", trusted.Highlights);
        Assert.Contains("2º mais vendido", trusted.Highlights);
        Assert.Empty(trusted.Warnings);
        Assert.Equal("Alta", OfficialCatalogSearchRules.SafetyLabel(trusted.SafetyScore));

        Assert.Equal(0, risky.SafetyScore);
        Assert.Equal("Baixa", OfficialCatalogSearchRules.SafetyLabel(risky.SafetyScore));
        Assert.Contains("Reputação vermelha", risky.Warnings);
        Assert.Contains("Produto usado", risky.Warnings);
    }

    [Fact]
    public void ApplySort_ShouldOrderByOpportunityBestSellersCommissionAndRating()
    {
        IReadOnlyList<OfficialCatalogProductDto> items =
        [
            new() { ProductId = "A", OpportunityScore = 40, BestSellerPosition = null, SoldQuantity = 50, EstimatedCommission = 10m, RatingAverage = 4.9m },
            new() { ProductId = "B", OpportunityScore = 90, BestSellerPosition = 7, EstimatedCommission = null, RatingAverage = 4.2m },
            new() { ProductId = "C", OpportunityScore = 60, BestSellerPosition = 1, EstimatedCommission = 30m, RatingAverage = null }
        ];

        Assert.Equal(["B", "C", "A"], OfficialCatalogSearchRules.ApplySort(items, "melhor_oportunidade").Select(item => item.ProductId));
        Assert.Equal(["C", "B", "A"], OfficialCatalogSearchRules.ApplySort(items, "mais_vendidos").Select(item => item.ProductId));
        Assert.Equal(["C", "A", "B"], OfficialCatalogSearchRules.ApplySort(items, "maior_comissao").Select(item => item.ProductId));
        Assert.Equal(["A", "B", "C"], OfficialCatalogSearchRules.ApplySort(items, "melhor_avaliacao").Select(item => item.ProductId));
    }

    [Fact]
    public void FilterLoadedResults_ShouldApplyOfficialTrustedAndBestSellerScreenFilters()
    {
        IReadOnlyList<OfficialCatalogProductDto> items =
        [
            new() { ProductId = "OFICIAL", IsOfficialStore = true, SafetyScore = 85 },
            new() { ProductId = "CONFIAVEL", SafetyScore = 75, BestSellerPosition = 4 },
            new() { ProductId = "RISCO", SafetyScore = 20 }
        ];

        Assert.Equal(["OFICIAL"], OfficialCatalogSearchRules.FilterLoadedResults(items, new OfficialCatalogSearchFilter { OfficialStoreOnly = true }).Select(item => item.ProductId));
        Assert.Equal(["OFICIAL", "CONFIAVEL"], OfficialCatalogSearchRules.FilterLoadedResults(items, new OfficialCatalogSearchFilter { TrustedSellersOnly = true }).Select(item => item.ProductId));
        Assert.Equal(["CONFIAVEL"], OfficialCatalogSearchRules.FilterLoadedResults(items, new OfficialCatalogSearchFilter { BestSellersOnly = true }).Select(item => item.ProductId));
    }

    [Fact]
    public void BuildMetrics_ShouldSummarizePricesSafetyRatingsAndHighlights()
    {
        IReadOnlyList<OfficialCatalogProductDto> items =
        [
            new() { ProductId = "A", Price = 1000m, Shipping = "Frete grátis", IsOfficialStore = true, SafetyScore = 90, OpportunityScore = 80, RatingAverage = 4.8m, SoldQuantity = 100, EstimatedCommission = 50m },
            new() { ProductId = "B", Price = 3000m, SafetyScore = 40, OpportunityScore = 30, RatingAverage = 4.0m, BestSellerPosition = 3 },
            new() { ProductId = "C", Price = 2000m, Shipping = "Frete grátis", SafetyScore = 75, OpportunityScore = 60, EstimatedCommission = 90m },
            new() { ProductId = "D", Price = 4000m, SafetyScore = 30, OpportunityScore = 20 }
        ];

        var metrics = OfficialCatalogSearchRules.BuildMetrics(items);

        Assert.Equal(4, metrics.Count);
        Assert.Equal(1000m, metrics.MinPrice);
        Assert.Equal(4000m, metrics.MaxPrice);
        Assert.Equal(2500m, metrics.MedianPrice);
        Assert.Equal(2500m, metrics.AveragePrice);
        Assert.Equal(50, metrics.FreeShippingPercent);
        Assert.Equal(25, metrics.OfficialStorePercent);
        Assert.Equal(50, metrics.TrustedSellerPercent);
        Assert.Equal(4.4m, metrics.AverageRating);
        Assert.Equal(100, metrics.TotalSoldKnown);
        Assert.Equal("A", metrics.BestOpportunity!.ProductId);
        Assert.Equal("B", metrics.BestSeller!.ProductId);
        Assert.Equal("C", metrics.TopCommission!.ProductId);
        Assert.Equal("A", metrics.Cheapest!.ProductId);
        Assert.Equal(0, OfficialCatalogSearchRules.BuildMetrics([]).Count);
    }

    [Fact]
    public void HumanizeMercadoLivreDomain_ShouldTurnDomainIdIntoReadableName()
    {
        Assert.Equal("Gaming headsets", OfficialCatalogSearchRules.HumanizeMercadoLivreDomain("MLB-GAMING_HEADSETS"));
        Assert.Equal("Notebooks", OfficialCatalogSearchRules.HumanizeMercadoLivreDomain("MLB-NOTEBOOKS"));
    }

    [Fact]
    public void DescribeMercadoLivreScan_ShouldShowOffersScannedAndShortenedTerm()
    {
        Assert.Equal("12 ofertas · 80 produtos analisados", OfficialCatalogSearchRules.DescribeMercadoLivreScan(12, 80, "dell", "Dell"));
        Assert.Equal("1 oferta · 20 produtos analisados", OfficialCatalogSearchRules.DescribeMercadoLivreScan(1, 20, null, "dell"));
        Assert.Equal(
            "0 ofertas · 100 produtos analisados (sem anúncio ativo)",
            OfficialCatalogSearchRules.DescribeMercadoLivreScan(0, 100, "dell i7", "dell i7"));
        Assert.Equal(
            "3 ofertas · 45 produtos analisados · buscado como \"dell inspiron\"",
            OfficialCatalogSearchRules.DescribeMercadoLivreScan(3, 45, "dell inspiron", "dell inspiron i7 16gb"));
    }

    [Fact]
    public void IsValidQuery_ShouldRequireTwoCharacters()
    {
        Assert.False(OfficialCatalogSearchRules.IsValidQuery(" "));
        Assert.False(OfficialCatalogSearchRules.IsValidQuery("a"));
        Assert.True(OfficialCatalogSearchRules.IsValidQuery("nb"));
    }

    [Fact]
    public void HasRealAffiliateCredentials_ShouldRejectSandboxPlaceholders()
    {
        Assert.False(OfficialCatalogSearchRules.HasRealAffiliateCredentials("100000", "tecflow-homolog-shopee-sandbox"));
        Assert.True(OfficialCatalogSearchRules.HasRealAffiliateCredentials("123456", "prod-secret"));
    }

    [Fact]
    public void CountVisibleOffers_ShouldApplyFiltersAndIgnoreDuplicateUrls()
    {
        OfficialCatalogProductDto Offer(string url, decimal price) => new() { SourceUrl = url, Price = price };
        var items = new[]
        {
            Offer("https://produto.mercadolivre.com.br/MLB-1?matt_tool=14343296", 100),
            Offer("https://produto.mercadolivre.com.br/MLB-1?matt_tool=14343296", 100),
            Offer("https://produto.mercadolivre.com.br/MLB-2?matt_tool=14343296", 900)
        };

        Assert.Equal(2, OfficialCatalogSearchRules.CountVisibleOffers(items, new OfficialCatalogSearchFilter()));
        Assert.Equal(1, OfficialCatalogSearchRules.CountVisibleOffers(items, new OfficialCatalogSearchFilter { MaxPrice = 500 }));
    }

    [Theory]
    [InlineData(System.Net.HttpStatusCode.Forbidden, "", true)]
    [InlineData(System.Net.HttpStatusCode.Unauthorized, "", true)]
    [InlineData(System.Net.HttpStatusCode.BadRequest, "{\"error\":\"unauthorized_scopes\"}", true)]
    [InlineData(System.Net.HttpStatusCode.InternalServerError, "{\"error\":\"internal\"}", false)]
    [InlineData(System.Net.HttpStatusCode.OK, "{\"results\":[]}", false)]
    public void IsMercadoLivreSearchRefused_ShouldDetectForbiddenAndScopeErrors(System.Net.HttpStatusCode status, string body, bool expected)
    {
        Assert.Equal(expected, OfficialCatalogSearchRules.IsMercadoLivreSearchRefused(status, body));
    }

    [Fact]
    public void EscapeGraphQl_ShouldEscapeQuotes()
    {
        Assert.Equal("fone \\\"bt\\\"", OfficialCatalogSearchRules.EscapeGraphQl("fone \"bt\""));
        Assert.Equal(20, OfficialCatalogSearchRules.ClampLimit(99));
    }

    [Fact]
    public void BuildChannelStatus_ShouldMapOkEmptyAndMissingCredentials()
    {
        var ok = OfficialCatalogSearchRules.BuildChannelStatus(
            "MercadoLivre", "Mercado Livre", true, 15, false, string.Empty);
        Assert.Equal("ok", ok.State);
        Assert.Equal("15 produtos", ok.Message);

        var empty = OfficialCatalogSearchRules.BuildChannelStatus(
            "MercadoLivre", "Mercado Livre", true, 0, false, string.Empty);
        Assert.Equal("empty", empty.State);
        Assert.Equal("0 produtos", empty.Message);

        var shopee = OfficialCatalogSearchRules.BuildChannelStatus(
            "Shopee",
            "Shopee",
            true,
            0,
            true,
            OfficialCatalogSearchRules.ShopeeMissingApiKeyMessage);
        Assert.Equal("missing", shopee.State);
        Assert.Equal("Chave de API não configurada", shopee.Message);

        var amazon = OfficialCatalogSearchRules.BuildChannelStatus(
            "Amazon",
            "Amazon",
            true,
            0,
            true,
            OfficialCatalogSearchRules.AmazonMissingPaApiMessage);
        Assert.Equal("PA-API não configurada", amazon.Message);

        var mlMissing = OfficialCatalogSearchRules.BuildChannelStatus(
            "MercadoLivre",
            "Mercado Livre",
            true,
            0,
            true,
            OfficialCatalogSearchRules.MercadoLivreMissingAccountMessage);
        Assert.Equal("missing", mlMissing.State);
        Assert.Equal("Requer OAuth do Mercado Livre para buscar ofertas", mlMissing.Message);

        var error = OfficialCatalogSearchRules.BuildChannelStatus(
            "MercadoLivre",
            "Mercado Livre",
            true,
            0,
            false,
            string.Empty,
            "Erro HTTP 403 (Forbidden)");
        Assert.Equal("error", error.State);
        Assert.Equal("Erro HTTP 403 (Forbidden)", error.Message);

        var recovered = OfficialCatalogSearchRules.BuildChannelStatus(
            "MercadoLivre",
            "Mercado Livre",
            true,
            12,
            false,
            string.Empty,
            "ML HTTP 403 (Forbidden)");
        Assert.Equal("ok", recovered.State);
        Assert.Equal("12 produtos", recovered.Message);
    }

    [Fact]
    public void MatchesMercadoLivreAccount_ShouldIgnoreSpacesInStoreName()
    {
        Assert.True(OfficialCatalogSearchRules.MatchesMercadoLivreAccount(
            MarketplaceType.MercadoLivre, "Loja", null));
        Assert.True(OfficialCatalogSearchRules.IsMercadoLivreLabel("Mercado Livre"));
        Assert.True(OfficialCatalogSearchRules.HasConnectedMercadoLivreStore(
            MarketplaceType.Shopee,
            "Mercado Livre",
            null,
            "14343296",
            null));
        Assert.Equal(
            "14343296",
            OfficialCatalogSearchRules.ResolveMercadoLivreAffiliateId(null, "14343296"));
    }

    [Fact]
    public void PassesOptionalFilters_ShouldNotRequireKeywordInTitle()
    {
        var item = new OfficialCatalogProductDto
        {
            ProductName = "Ultrabook Gamer",
            Price = 10,
            SourceUrl = "https://www.mercadolivre.com.br/p/MLB1"
        };

        Assert.True(OfficialCatalogSearchRules.PassesOptionalFilters(
            item,
            new OfficialCatalogSearchFilter { Keyword = "notebook 16GB" }));
        Assert.False(OfficialCatalogSearchRules.PassesOptionalFilters(
            item,
            new OfficialCatalogSearchFilter { MinPrice = 50 }));
    }
}
