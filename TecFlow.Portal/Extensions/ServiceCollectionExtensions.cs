using TecFlow.Portal.Configuration;
using TecFlow.Portal.Services.Auth;
using TecFlow.Portal.Services.Dashboard;
using TecFlow.Portal.Services.Http;
using TecFlow.Portal.Services.State;
using TecFlow.Portal.Services.UI;

namespace TecFlow.Portal.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPortalServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OrquestradorApiOptions>(configuration.GetSection(OrquestradorApiOptions.SectionName));

        services.AddHttpClient("Orquestrador", (sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OrquestradorApiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });

        services.AddScoped<IHttpService, HttpService>();
        services.AddScoped<IOrquestradorAuthBridge, OrquestradorAuthBridge>();
        services.AddScoped<IAuthCookieService, AuthCookieService>();
        services.AddSingleton<IOAuthProviderRegistry, OAuthProviderRegistry>();
        services.AddScoped<ITikTokAuthService, TikTokAuthService>();
        services.AddScoped<IShopeeAuthService, ShopeeAuthService>();
        services.AddScoped<ISessionStateService, SessionStateService>();
        services.AddScoped<ILoadingService, LoadingService>();
        services.AddScoped<IDashboardApiService, DashboardApiService>();

        return services;
    }
}
