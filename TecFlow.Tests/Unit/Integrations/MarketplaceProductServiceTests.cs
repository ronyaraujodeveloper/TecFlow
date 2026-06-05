using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TecFlow.Business.Integrations.Auth;
using TecFlow.Business.Integrations.Catalog;
using TecFlow.Business.Integrations.Shopee;
using TecFlow.Business.Integrations.Shopee.Payloads;
using TecFlow.Business.Integrations.TikTokShop;
using TecFlow.Business.Integrations.TikTokShop.Payloads;
using TecFlow.Core.Enums;
using TecFlow.Infrastructure.Services.Integrations.Catalog;
using TecFlow.Tests.Helpers;

namespace TecFlow.Tests.Unit.Integrations;

public class MarketplaceProductServiceTests
{
    private readonly MarketplaceProductService _service;

    public MarketplaceProductServiceTests()
    {
        _service = new MarketplaceProductService(
            new Mock<IMarketplaceAuthService>().Object,
            new Mock<IMarketplaceSignatureService>().Object,
            new Mock<IShopeeIntegrationClient>().Object,
            new Mock<ITikTokShopIntegrationClient>().Object,
            MarketplaceTestOptionsFactory.ShopeeOptions(),
            MarketplaceTestOptionsFactory.TikTokOptions(),
            NullLogger<MarketplaceProductService>.Instance);
    }

    [Fact]
    public void ConvertToInternalProductDto_ShouldMapShopeeItemToProductResponseDto_WhenPayloadIsValid()
    {
        // Arrange
        var payload = new ShopeeGetItemBaseInfoResponsePayload
        {
            ItemList =
            [
                new ShopeeItemBaseInfo
                {
                    ItemId = 1001,
                    ItemName = "Camiseta",
                    Description = "Descrição longa da camiseta",
                    ItemSku = "CAM-001",
                    CategoryId = 42,
                    HasModel = false,
                    PriceInfo = [new ShopeePriceInfo { CurrentPrice = 49.90m }],
                    StockInfoV2 = new ShopeeStockInfoV2
                    {
                        SummaryInfo = new ShopeeStockSummaryInfo { TotalAvailableStock = 15 }
                    }
                }
            ]
        };

        // Act
        var result = _service.ConvertToInternalProductDto(MarketplaceType.Shopee, payload);

        // Assert
        Assert.True(result.Status);
        Assert.NotNull(result.DataList);
        var product = Assert.Single(result.DataList!);
        Assert.Equal("1001", product.ExternalProductId);
        Assert.Equal("CAM-001", product.SkuCode);
        Assert.Equal("Camiseta", product.Name);
        Assert.Equal(49.90m, product.Price);
        Assert.Equal(15, product.Stock);
        Assert.Equal(MarketplaceType.Shopee, product.MarketplaceSource);
    }

    [Fact]
    public void ConvertToInternalProductDto_ShouldExpandShopeeVariants_WhenHasModelIsTrue()
    {
        // Arrange
        const long itemId = 2002;
        var payload = new ShopeeGetItemBaseInfoResponsePayload
        {
            ItemList =
            [
                new ShopeeItemBaseInfo
                {
                    ItemId = itemId,
                    ItemName = "Tênis",
                    HasModel = true,
                    PriceInfo = [new ShopeePriceInfo { CurrentPrice = 199m }]
                }
            ]
        };

        var models = new Dictionary<long, ShopeeGetModelListResponsePayload>
        {
            [itemId] = new()
            {
                Model =
                [
                    new ShopeeProductModel
                    {
                        ModelId = 11,
                        ModelSku = "TENIS-41",
                        PriceInfo = [new ShopeePriceInfo { CurrentPrice = 189m }],
                        StockInfoV2 = new ShopeeStockInfoV2
                        {
                            SummaryInfo = new ShopeeStockSummaryInfo { TotalAvailableStock = 3 }
                        }
                    },
                    new ShopeeProductModel
                    {
                        ModelId = 12,
                        ModelSku = "TENIS-42",
                        StockInfoV2 = new ShopeeStockInfoV2
                        {
                            SummaryInfo = new ShopeeStockSummaryInfo { TotalAvailableStock = 5 }
                        }
                    }
                ]
            }
        };

        // Act
        var result = _service.ConvertToInternalProductDto(MarketplaceType.Shopee, payload, models);

        // Assert
        Assert.Equal(2, result.DataList!.Count);
        Assert.Contains(result.DataList, p => p.SkuCode == "TENIS-41" && p.Stock == 3);
        Assert.Contains(result.DataList, p => p.SkuCode == "TENIS-42" && p.Stock == 5);
    }

    [Fact]
    public void ConvertToInternalProductDto_ShouldMapTikTokSkusToProductResponseDto_WhenSearchPayloadHasSkus()
    {
        // Arrange
        var payload = new TikTokShopProductsSearchResponsePayload
        {
            Products =
            [
                new TikTokShopProduct
                {
                    Id = "prod-88",
                    Title = "Fone Bluetooth",
                    Description = "Fone com ANC",
                    CategoryChains =
                    [
                        new TikTokShopCategoryChain { LocalName = "Eletrônicos", IsLeaf = false },
                        new TikTokShopCategoryChain { LocalName = "Áudio", IsLeaf = true }
                    ],
                    Skus =
                    [
                        new TikTokShopProductSku
                        {
                            Id = "sku-1",
                            SellerSku = "FONE-BLK",
                            Price = new TikTokShopSkuPrice { TaxExclusivePrice = "129.90" },
                            Inventory = [new TikTokShopSkuInventory { AvailableStock = 7 }]
                        }
                    ]
                }
            ]
        };

        // Act
        var result = _service.ConvertToInternalProductDto(MarketplaceType.TikTokShop, payload);

        // Assert
        var product = Assert.Single(result.DataList!);
        Assert.Equal("prod-88", product.ExternalProductId);
        Assert.Equal("FONE-BLK", product.SkuCode);
        Assert.Equal("Fone Bluetooth", product.Name);
        Assert.Equal(129.90m, product.Price);
        Assert.Equal(7, product.Stock);
        Assert.Equal("Áudio", product.Category);
        Assert.Equal(MarketplaceType.TikTokShop, product.MarketplaceSource);
    }

    [Fact]
    public async Task FetchProductsFromPlatformAsync_ShouldReturnFailure_WhenShopIdIsEmpty()
    {
        // Act
        var result = await _service.FetchProductsFromPlatformAsync("", MarketplaceType.Shopee, 1, 10);

        // Assert
        Assert.False(result.Status);
        Assert.Contains("shopId", result.Descricao, StringComparison.OrdinalIgnoreCase);
    }
}
