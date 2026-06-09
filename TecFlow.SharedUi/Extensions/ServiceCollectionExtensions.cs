using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TecFlow.SharedUi.Configuration;
using TecFlow.SharedUi.Services.Auth;
using TecFlow.SharedUi.Services.Advertising;
using TecFlow.SharedUi.Services.Health;
using TecFlow.SharedUi.Services.Analytics;
using TecFlow.SharedUi.Services.Dashboard;
using TecFlow.SharedUi.Services.Http;
using TecFlow.SharedUi.Services.State;
using TecFlow.SharedUi.Services.Navigation;
using TecFlow.SharedUi.Services.UI;
using TecFlow.SharedUi.Services.Devices;
using TecFlow.SharedUi.Services.Integrations;
using TecFlow.SharedUi.Services.LinkGenerator;

namespace TecFlow.SharedUi.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTecFlowClientServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OrquestradorApiOptions>(configuration.GetSection(OrquestradorApiOptions.SectionName));

        services.AddHttpClient("Orquestrador", (sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OrquestradorApiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });

        services.AddScoped<IAccessTokenProvider, SessionAccessTokenProvider>();
        services.AddScoped<IHttpService, HttpService>();
        services.AddScoped<IOrquestradorAuthBridge, OrquestradorAuthBridge>();
        services.AddSingleton<IOAuthProviderRegistry, OAuthProviderRegistry>();
        services.AddScoped<ITikTokAuthService, TikTokAuthService>();
        services.AddScoped<IShopeeAuthService, ShopeeAuthService>();
        services.AddScoped<IUserRegistrationApiService, UserRegistrationApiService>();
        services.AddScoped<IAccountSecurityApiService, AccountSecurityApiService>();
        services.AddScoped<IIntegracaoLojaApiService, IntegracaoLojaApiService>();
        services.AddScoped<IAffiliateLinkApiService, AffiliateLinkApiService>();
        services.AddScoped<ISessionStateService, SessionStateService>();
        services.AddScoped<IActiveStoreScopeService, NullActiveStoreScopeService>();
        services.AddScoped<ILoadingService, LoadingService>();
        services.AddScoped<IDashboardApiService, DashboardApiService>();
        services.AddScoped<IAffiliateAnalyticsApiService, AffiliateAnalyticsApiService>();
        services.AddScoped<IAdvertisingProductApiService, AdvertisingProductApiService>();
        services.AddScoped<IPlatformHealthApiService, PlatformHealthApiService>();
        services.AddSingleton<INavigationIntentService, NavigationIntentService>();
        services.AddScoped<IDeviceRegistrationApiService, DeviceRegistrationApiService>();

        return services;
    }
}
