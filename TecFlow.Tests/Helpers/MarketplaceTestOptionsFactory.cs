using Microsoft.Extensions.Options;
using TecFlow.Business.Integrations.Shopee;
using TecFlow.Business.Integrations.TikTokShop;

namespace TecFlow.Tests.Helpers;

public static class MarketplaceTestOptionsFactory
{
    public const string ShopeePartnerId = "100001";
    public const string ShopeePartnerKey = "shopee-test-partner-key";
    public const string TikTokAppKey = "tiktok-test-app-key";
    public const string TikTokAppSecret = "tiktok-test-app-secret";
    public const string WebhookCallbackUrl = "https://api.tecflow.test/webhooks/shopee";

    public static IOptions<ShopeeIntegrationOptions> ShopeeOptions(string? webhookUrl = null) =>
        Options.Create(new ShopeeIntegrationOptions
        {
            PartnerId = ShopeePartnerId,
            PartnerKey = ShopeePartnerKey,
            ApiBaseUrl = "https://partner.shopeemobile.com/api/v2/",
            AuthPartnerPath = "shop/auth_partner",
            TokenPath = "auth/token/get",
            RefreshTokenPath = "auth/access_token/get",
            GetItemListPath = "product/get_item_list",
            GetItemBaseInfoPath = "product/get_item_base_info",
            GetModelListPath = "product/get_model_list",
            GetOrderListPath = "order/get_order_list",
            GetOrderDetailPath = "order/get_order_detail",
            UpdateStockPath = "product/update_stock",
            WebhookCallbackUrl = webhookUrl ?? WebhookCallbackUrl
        });

    public static IOptions<TikTokShopIntegrationOptions> TikTokOptions(string? webhookSecret = null) =>
        Options.Create(new TikTokShopIntegrationOptions
        {
            AppKey = TikTokAppKey,
            AppSecret = TikTokAppSecret,
            ApiBaseUrl = "https://open-api.tiktokglobalshop.com",
            AuthorizeUrl = "https://auth.tiktok-shops.com/oauth/authorize",
            TokenUrl = "https://auth.tiktok-shops.com/api/v2/token/get",
            RefreshTokenUrl = "https://auth.tiktok-shops.com/api/v2/token/refresh",
            ProductsSearchPath = "api/v1/products/search",
            OrdersSearchPath = "api/v1/orders/search",
            UpdateStocksPath = "api/v1/products/stocks",
            WebhookSecret = webhookSecret
        });
}
