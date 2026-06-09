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
                AllowAutoRedirect = false
            })
            .ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(15);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("TecFlow/1.0 (+https://tecflow.local)");
            });

        services.AddScoped<IUrlExpansionService, UrlExpansionService>();
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
}
