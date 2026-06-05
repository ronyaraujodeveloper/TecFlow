using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TecFlow.Business.Integrations.Common;
using TecFlow.Business.Integrations.Shopee;
using TecFlow.Business.Integrations.TikTokShop;
using TecFlow.Infrastructure.Services.Integrations.Common;
using TecFlow.Infrastructure.Services.Integrations.Shopee;
using TecFlow.Infrastructure.Services.Integrations.TikTokShop;

namespace TecFlow.Infrastructure.Services.Integrations;

public static class IntegrationHttpClientRegistrationExtensions
{
    public static IServiceCollection AddTecFlowIntegrationHttpClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<IntegrationResilienceOptions>(
            configuration.GetSection(IntegrationResilienceOptions.SectionName));
        services.Configure<TikTokShopIntegrationOptions>(
            configuration.GetSection(TikTokShopIntegrationOptions.SectionName));
        services.Configure<ShopeeIntegrationOptions>(
            configuration.GetSection(ShopeeIntegrationOptions.SectionName));

        var resilience = configuration
            .GetSection(IntegrationResilienceOptions.SectionName)
            .Get<IntegrationResilienceOptions>() ?? new IntegrationResilienceOptions();

        RegisterTikTokShopClient(services, resilience);
        RegisterShopeeClient(services, resilience);

        return services;
    }

    private static void RegisterTikTokShopClient(
        IServiceCollection services,
        IntegrationResilienceOptions resilience)
    {
        services.AddHttpClient<ITikTokShopIntegrationClient, TikTokShopIntegrationClient>(
                IntegrationHttpClientNames.TikTokShop)
            .ConfigureHttpClient((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<TikTokShopIntegrationOptions>>().Value;
                ConfigureBaseAddress(client, options.ApiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 10, 300));
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .AddHttpMessageHandler(sp =>
                CreateLoggingHandler(sp, "TikTokShop", sp.GetRequiredService<IOptions<TikTokShopIntegrationOptions>>().Value.EnableRequestLogging))
            .AddPolicyHandler(IntegrationResiliencePolicies.CreateRetryPolicy(resilience))
            .AddPolicyHandler(IntegrationResiliencePolicies.CreateCircuitBreakerPolicy(resilience))
            .SetHandlerLifetime(TimeSpan.FromMinutes(5));
    }

    private static void RegisterShopeeClient(
        IServiceCollection services,
        IntegrationResilienceOptions resilience)
    {
        services.AddHttpClient<IShopeeIntegrationClient, ShopeeIntegrationClient>(
                IntegrationHttpClientNames.Shopee)
            .ConfigureHttpClient((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<ShopeeIntegrationOptions>>().Value;
                ConfigureBaseAddress(client, options.ApiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 10, 300));
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .AddHttpMessageHandler(sp =>
                CreateLoggingHandler(sp, "Shopee", sp.GetRequiredService<IOptions<ShopeeIntegrationOptions>>().Value.EnableRequestLogging))
            .AddPolicyHandler(IntegrationResiliencePolicies.CreateRetryPolicy(resilience))
            .AddPolicyHandler(IntegrationResiliencePolicies.CreateCircuitBreakerPolicy(resilience))
            .SetHandlerLifetime(TimeSpan.FromMinutes(5));
    }

    private static DelegatingHandler CreateLoggingHandler(
        IServiceProvider sp,
        string platformName,
        bool enableLogging)
    {
        if (!enableLogging)
        {
            return new PassThroughHandler();
        }

        var logger = sp.GetRequiredService<ILogger<ExternalApiLoggingHandler>>();
        return new ExternalApiLoggingHandler(logger, platformName);
    }

    private static void ConfigureBaseAddress(HttpClient client, string apiBaseUrl)
    {
        if (!string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
        }
    }

    private sealed class PassThroughHandler : DelegatingHandler;
}
