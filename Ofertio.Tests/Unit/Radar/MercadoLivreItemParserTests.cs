using Ofertio.Core.Entities;
using Ofertio.Infrastructure.Services.Radar;

namespace Ofertio.Tests.Unit.Radar;

public class MercadoLivreItemParserTests
{
    [Fact]
    public void ParseCatalogSearchIds_ShouldKeepRelevanceOrderWithoutDuplicates()
    {
        const string json = """
            {"keywords":"dell i7","paging":{"total":3},"results":[{"id":"MLB200","name":"A"},{"id":"MLB100","name":"B"},{"id":"MLB200","name":"A"},{"name":"sem id"}]}
            """;

        Assert.Equal(["MLB200", "MLB100"], MercadoLivreItemParser.ParseCatalogSearchIds(json));
        Assert.Empty(MercadoLivreItemParser.ParseCatalogSearchIds("""{"message":"forbidden"}"""));
    }

    [Fact]
    public void ParseCatalogSearchPage_ShouldReadTotalAndDomains()
    {
        const string json = """
            {"paging":{"total":437,"offset":20,"limit":20},"results":[{"id":"MLB1","domain_id":"MLB-NOTEBOOKS"},{"id":"MLB2","domain_id":"MLB-NOTEBOOKS"},{"id":"MLB3"}]}
            """;

        var page = MercadoLivreItemParser.ParseCatalogSearchPage(json);

        Assert.Equal(["MLB1", "MLB2", "MLB3"], page.ProductIds);
        Assert.Equal(437, page.Total);
        Assert.Equal(["MLB-NOTEBOOKS", "MLB-NOTEBOOKS"], page.DomainIds);
        Assert.Null(MercadoLivreItemParser.ParseCatalogSearchPage("not json").Total);
        Assert.Empty(MercadoLivreItemParser.ParseCatalogSearchPage(string.Empty).ProductIds);
    }

    [Fact]
    public void ParseCatalogOffer_ShouldMapSellerListingAndSafetySignals()
    {
        const string json = """
            {"id":"MLB5","name":"Notebook","domain_id":"MLB-NOTEBOOKS","buy_box_winner":{"item_id":"MLB555","price":3999,"seller_id":123456,"official_store_id":789,"category_id":"MLB1652","warranty":"Garantia de fábrica: 12 meses","condition":"new","shipping":{"free_shipping":true,"logistic_type":"fulfillment"}}}
            """;

        var item = MercadoLivreItemParser.ParseCatalogOffer(json);

        Assert.NotNull(item);
        Assert.Equal("MLB555", item!.ListingId);
        Assert.Equal("123456", item.SellerId);
        Assert.Equal("MLB1652", item.CategoryId);
        Assert.True(item.IsOfficialStore);
        Assert.True(item.IsFulfillment);
        Assert.Equal("new", item.Condition);
        Assert.Equal("Garantia de fábrica: 12 meses", item.Warranty);
    }

    [Fact]
    public void ParseItemsMultiget_ShouldReadSoldQuantityAndSkipFailedEntries()
    {
        const string json = """
            [{"code":200,"body":{"id":"MLB1","sold_quantity":250,"seller_id":42,"official_store_id":null,"condition":"new","shipping":{"logistic_type":"cross_docking"},"category_id":"MLB1652"}},
             {"code":200,"body":{"id":"MLB2","sold_quantity":0}},
             {"code":404,"body":{"id":"MLB3","message":"not found"}}]
            """;

        var listings = MercadoLivreItemParser.ParseItemsMultiget(json);

        Assert.Equal(2, listings.Count);
        Assert.Equal(250, listings["MLB1"].SoldQuantity);
        Assert.Equal("42", listings["MLB1"].SellerId);
        Assert.False(listings["MLB1"].IsOfficialStore);
        Assert.False(listings["MLB1"].IsFulfillment);
        Assert.Null(listings["MLB2"].SoldQuantity);
        Assert.Empty(MercadoLivreItemParser.ParseItemsMultiget("""{"message":"forbidden"}"""));
    }

    [Fact]
    public void ParseSeller_ShouldReadReputationPowerStatusAndRatings()
    {
        const string json = """
            {"id":42,"nickname":"LOJA.DELL","seller_reputation":{"level_id":"5_green","power_seller_status":"platinum","transactions":{"completed":15230,"ratings":{"positive":0.98,"negative":0.01,"neutral":0.01}}}}
            """;

        var seller = MercadoLivreItemParser.ParseSeller(json);

        Assert.Equal(new MercadoLivreSellerMetrics("LOJA.DELL", "5_green", "platinum", 15230, 0.98m), seller);
        Assert.Null(MercadoLivreItemParser.ParseSeller("""{"id":42,"nickname":"SEM"}"""));
    }

    [Fact]
    public void ParseReviewSummary_ShouldReadAverageAndTotal()
    {
        Assert.Equal(
            new MercadoLivreReviewSummary(4.7m, 1234),
            MercadoLivreItemParser.ParseReviewSummary("""{"paging":{"total":1234},"rating_average":4.68,"reviews":[]}"""));
        Assert.Null(MercadoLivreItemParser.ParseReviewSummary("""{"paging":{"total":0},"rating_average":0}"""));
    }

    [Fact]
    public void ParseCategory_ShouldReadNameAndRootFromPathFromRoot()
    {
        Assert.Equal(
            new MercadoLivreCategoryPath("Notebooks", "MLB1648", "Informática"),
            MercadoLivreItemParser.ParseCategory("""{"id":"MLB1652","name":"Notebooks","path_from_root":[{"id":"MLB1648","name":"Informática"},{"id":"MLB1652","name":"Notebooks"}]}"""));
        Assert.Equal(
            new MercadoLivreCategoryPath("Informática", "MLB1648", "Informática"),
            MercadoLivreItemParser.ParseCategory("""{"id":"MLB1648","name":"Informática"}"""));
        Assert.Null(MercadoLivreItemParser.ParseCategory("""{"message":"Category not found","error":"not_found"}"""));
    }

    [Fact]
    public void ParseCatalogOffer_ShouldCollectAllProductPicturesForTheGallery()
    {
        const string json = """
            {"id":"MLB1","name":"Fone","pictures":[{"url":"http://http2.mlstatic.com/a.jpg"},{"url":"https://http2.mlstatic.com/b.jpg"},{"url":"https://http2.mlstatic.com/b.jpg"}],"buy_box_winner":{"item_id":"MLB9","price":99}}
            """;

        var item = MercadoLivreItemParser.ParseCatalogOffer(json);

        Assert.NotNull(item);
        Assert.Equal("https://http2.mlstatic.com/a.jpg", item!.ImageUrl);
        Assert.Equal(["https://http2.mlstatic.com/a.jpg", "https://http2.mlstatic.com/b.jpg"], item.ImageUrls);
    }

    [Fact]
    public void ParseBestSellerPositions_ShouldMapProductsAndItemsToRankingPosition()
    {
        const string json = """
            {"query_data":{"highlight_type":"BEST_SELLER","criteria":"CATEGORY","id":"MLB1652"},"content":[{"id":"MLB10","position":1,"type":"PRODUCT"},{"id":"MLB777","position":2,"type":"ITEM"},{"id":"MLB10","position":9,"type":"PRODUCT"}]}
            """;

        var positions = MercadoLivreItemParser.ParseBestSellerPositions(json);

        Assert.Equal(1, positions["MLB10"]);
        Assert.Equal(2, positions["MLB777"]);
        Assert.Empty(MercadoLivreItemParser.ParseBestSellerPositions("""{"message":"forbidden"}"""));
    }

    [Fact]
    public void ParseDomainDiscovery_ShouldMapDomainsWithNamesWithoutDuplicates()
    {
        const string json = """
            [{"domain_id":"MLB-NOTEBOOKS","domain_name":"Notebooks","category_id":"MLB1652","category_name":"Notebooks"},
             {"domain_id":"MLB-NOTEBOOKS","domain_name":"Notebooks"},
             {"domain_id":"MLB-DESKTOP_COMPUTERS","category_name":"PCs"},
             {"category_name":"sem domínio"}]
            """;

        var categories = MercadoLivreItemParser.ParseDomainDiscovery(json);

        Assert.Equal(["MLB-NOTEBOOKS", "MLB-DESKTOP_COMPUTERS"], categories.Select(category => category.Id));
        Assert.Equal(["Notebooks", "PCs"], categories.Select(category => category.Name));
        Assert.Empty(MercadoLivreItemParser.ParseDomainDiscovery("""{"message":"forbidden"}"""));
    }

    [Fact]
    public void ParseCatalogOffer_ShouldMapBuyBoxWinnerPriceAndProductPage()
    {
        const string json = """
            {"id":"MLB123","name":"Notebook Gamer","permalink":"https://www.mercadolivre.com.br/notebook-gamer/p/MLB123","pictures":[{"url":"http://http2.mlstatic.com/x.jpg"}],"buy_box_winner":{"item_id":"MLB999","price":1999.9,"original_price":2499,"shipping":{"free_shipping":true}}}
            """;

        var item = MercadoLivreItemParser.ParseCatalogOffer(json);

        Assert.NotNull(item);
        Assert.Equal("MLB123", item!.ProductId);
        Assert.Equal("Notebook Gamer", item.ProductName);
        Assert.Equal(1999.9m, item.Price);
        Assert.Equal(2499m, item.OriginalPrice);
        Assert.Equal("https://www.mercadolivre.com.br/notebook-gamer/p/MLB123", item.SourceUrl);
        Assert.Equal("https://http2.mlstatic.com/x.jpg", item.ImageUrl);
        Assert.Equal("Frete grátis", item.Shipping);
    }

    [Theory]
    [InlineData("""{"id":"MLB123","name":"Sem oferta","buy_box_winner":null}""")]
    [InlineData("""{"id":"MLB123","name":"Sem preço","buy_box_winner":{"item_id":"MLB9"}}""")]
    [InlineData("""{"message":"forbidden","error":"forbidden","status":403}""")]
    public void ParseCatalogOffer_WithoutActiveBuyBoxOffer_ShouldReturnNull(string json)
    {
        Assert.Null(MercadoLivreItemParser.ParseCatalogOffer(json));
    }

    [Fact]
    public void ParseCatalogOffer_WithoutWinner_ShouldUseMinimumOfPriceRange()
    {
        var item = MercadoLivreItemParser.ParseCatalogOffer(
            """{"id":"MLB7","name":"Notebook","buy_box_winner":null,"buy_box_winner_price_range":{"min":{"price":3500,"currency_id":"BRL"},"max":{"price":4200}}}""");

        Assert.Equal(3500m, item!.Price);
        Assert.Equal("https://www.mercadolivre.com.br/p/MLB7", item.SourceUrl);
    }

    [Fact]
    public void ParseCatalogItemsOffer_ShouldPickCheapestPricedItem()
    {
        var item = MercadoLivreItemParser.ParseCatalogItemsOffer(
            """{"id":"MLB7","name":"Notebook","pictures":[{"url":"https://http2.mlstatic.com/n.jpg"}]}""",
            """{"results":[{"item_id":"MLB71","price":4999},{"item_id":"MLB72"},{"item_id":"MLB73","price":4599.9,"original_price":5200,"shipping":{"free_shipping":true}}]}""");

        Assert.Equal(4599.9m, item!.Price);
        Assert.Equal(5200m, item.OriginalPrice);
        Assert.Equal("Notebook", item.ProductName);
        Assert.Equal("Frete grátis", item.Shipping);
        Assert.Null(MercadoLivreItemParser.ParseCatalogItemsOffer("""{"id":"MLB7"}""", """{"results":[]}"""));
    }

    [Fact]
    public void ParseCatalogChildrenIds_ShouldReturnChildrenOfParentProduct()
    {
        Assert.Equal(
            ["MLB31", "MLB32"],
            MercadoLivreItemParser.ParseCatalogChildrenIds("""{"id":"MLB30","children_ids":["MLB31","MLB32","MLB31"]}"""));
        Assert.Empty(MercadoLivreItemParser.ParseCatalogChildrenIds("""{"id":"MLB31","children_ids":[]}"""));
    }

    [Theory]
    [InlineData("""{"id":"MLB1","status":"active","available_quantity":0,"price":10}""", false, GroupOfferStatuses.Esgotado)]
    [InlineData("""{"id":"MLB1","status":"paused","available_quantity":4,"price":10}""", false, GroupOfferStatuses.Inativo)]
    [InlineData("""{"id":"MLB1","status":"active","available_quantity":4,"price":10}""", true, GroupOfferStatuses.Ativo)]
    public void Parse_ShouldMarkSoldOutAndPausedItemsUnavailable(string json, bool available, string status)
    {
        var item = MercadoLivreItemParser.Parse(json);

        Assert.NotNull(item);
        Assert.Equal(available, item.IsAvailable);
        Assert.Equal(status, item.Status);
    }
}
