using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TecFlow.Business.Integrations.Common;
using TecFlow.Business.Integrations.Shopee;
using TecFlow.Business.Integrations.TikTokShop;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.Infrastructure.Services.LinkStrategies;

public static class AffiliateLinkInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddAffiliateLinkInfrastructureServices(this IServiceCollection services)
    {
        services.AddHttpClient(IntegrationHttpClientNames.UrlExpansion)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 10
            })
            .ConfigureHttpClient(ConfigureExpansionClient);

        services.AddHttpClient(IntegrationHttpClientNames.UrlExpansionFollow)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 10
            })
            .ConfigureHttpClient(ConfigureExpansionClient);

        services.AddHttpClient(IntegrationHttpClientNames.ProductMetadata)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 10
            })
            .ConfigureHttpClient(ConfigureAntiBotBrowserClient);

        services.AddScoped<IUrlExpansionService, UrlExpansionService>();
        services.AddScoped<IProductMetadataService, ProductMetadataService>();
        services.AddScoped<IIntegracaoLojaScopeResolver, IntegracaoLojaScopeResolver>();

        services.AddHttpClient<IShopeeAffiliateLinkClient, ShopeeAffiliateLinkClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<ShopeeIntegrationOptions>>().Value;
            client.BaseAddress = new Uri(options.AffiliateApiBaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        services.AddHttpClient<ITikTokAffiliateLinkClient, TikTokAffiliateLinkClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<TikTokShopIntegrationOptions>>().Value;
            client.BaseAddress = new Uri(options.ApiBaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        return services;
    }

    private static void ConfigureExpansionClient(HttpClient client)
    {
        client.Timeout = TimeSpan.FromSeconds(15);
        ApplyAntiBotBrowserHeaders(client);
        client.DefaultRequestHeaders.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");
    }

    private static void ConfigureAntiBotBrowserClient(HttpClient client)
    {
        client.Timeout = TimeSpan.FromSeconds(10);
        ApplyAntiBotBrowserHeaders(client);
    }

    private static void ApplyAntiBotBrowserHeaders(HttpClient client)
    {
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.TryAddWithoutValidation(
            "User-Agent",
            "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1");
        client.DefaultRequestHeaders.TryAddWithoutValidation(
            "Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "pt-BR,pt;q=0.9,en-US;q=0.8");
    }
}
