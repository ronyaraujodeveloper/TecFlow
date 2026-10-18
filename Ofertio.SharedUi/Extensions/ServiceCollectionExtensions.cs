using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ofertio.SharedUi.Configuration;
using Ofertio.SharedUi.Services.Auth;
using Ofertio.SharedUi.Services.Advertising;
using Ofertio.SharedUi.Services.Health;
using Ofertio.SharedUi.Services.Analytics;
using Ofertio.SharedUi.Services.Dashboard;
using Ofertio.SharedUi.Services.Http;
using Ofertio.SharedUi.Services.State;
using Ofertio.SharedUi.Services.Navigation;
using Ofertio.SharedUi.Services.UI;
using Ofertio.SharedUi.Services.Devices;
using Ofertio.SharedUi.Services.Integrations;
using Ofertio.SharedUi.Services.LinkGenerator;
using Ofertio.SharedUi.Services.Radar;

namespace Ofertio.SharedUi.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOfertioClientServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment = null)
    {
        services.Configure<OrquestradorApiOptions>(configuration.GetSection(OrquestradorApiOptions.SectionName));

        var orquestradorClient = services.AddHttpClient("Orquestrador", (sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OrquestradorApiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = Ofertio.Business.Service.Groups.MonitoredGroupSyncRules.ResolveHttpTimeout(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });

        if (environment is not null
            && (environment.IsDevelopment() || environment.IsEnvironment("Homologacao")))
        {
#pragma warning disable CA1416
            orquestradorClient.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });
#pragma warning restore CA1416
        }

        services.AddScoped<IAccessTokenProvider, SessionAccessTokenProvider>();
        services.AddScoped<IHttpService, HttpService>();
        services.AddScoped<IOrquestradorAuthBridge, OrquestradorAuthBridge>();
        services.AddSingleton<IOAuthProviderRegistry, OAuthProviderRegistry>();
        services.AddScoped<ITikTokAuthService, TikTokAuthService>();
        services.AddScoped<IShopeeAuthService, ShopeeAuthService>();
        services.AddScoped<IUserRegistrationApiService, UserRegistrationApiService>();
        services.AddScoped<IAccountSecurityApiService, AccountSecurityApiService>();
        services.AddScoped<IIntegracaoLojaApiService, IntegracaoLojaApiService>();
        services.AddScoped<IMarketplaceOAuthConnectService, MarketplaceOAuthConnectService>();
        services.AddScoped<IAffiliateLinkApiService, AffiliateLinkApiService>();
        services.AddScoped<IPublicConverterApiService, PublicConverterApiService>();
        services.AddScoped<IWhatsAppIntegrationApiService, WhatsAppIntegrationApiService>();
        services.AddScoped<ITelegramIntegrationApiService, TelegramIntegrationApiService>();
        services.AddScoped<IMonitoredGroupsApiService, MonitoredGroupsApiService>();
        services.AddScoped<IOfferRadarApiService, OfferRadarApiService>();
        services.AddScoped<IOfferIntelligenceApiService, OfferIntelligenceApiService>();
        services.AddScoped<IPreFlightApiService, PreFlightApiService>();
        services.AddScoped<IDealIntelligenceApiService, DealIntelligenceApiService>();
        services.AddScoped<IOfficialCatalogSearchApiService, OfficialCatalogSearchApiService>();
        services.AddScoped<Ofertio.SharedUi.Services.Master.IMasterApiService, Ofertio.SharedUi.Services.Master.MasterApiService>();
        services.AddScoped<Ofertio.SharedUi.Services.Master.ISystemCommunicationApiService, Ofertio.SharedUi.Services.Master.SystemCommunicationApiService>();
        services.AddScoped<ISessionStateService, SessionStateService>();
        services.AddScoped<IActiveStoreScopeService, NullActiveStoreScopeService>();
        services.AddScoped<ILoadingService, LoadingService>();
        services.AddScoped<IModalService, ModalService>();
        services.AddScoped<IToastService, ToastService>();
        services.AddScoped<IDashboardApiService, DashboardApiService>();
        services.AddScoped<IAffiliateAnalyticsApiService, AffiliateAnalyticsApiService>();
        services.AddScoped<IAdvertisingProductApiService, AdvertisingProductApiService>();
        services.AddScoped<IPlatformHealthApiService, PlatformHealthApiService>();
        services.AddSingleton<INavigationIntentService, NavigationIntentService>();
        services.AddScoped<IDeviceRegistrationApiService, DeviceRegistrationApiService>();

        return services;
    }
}
