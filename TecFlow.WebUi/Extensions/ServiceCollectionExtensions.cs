using TecFlow.WebUi.Configuration;
using TecFlow.WebUi.Services.Auth;
using TecFlow.WebUi.Services.Dashboard;
using TecFlow.WebUi.Services.Http;
using TecFlow.WebUi.Services.State;
using TecFlow.WebUi.Services.UI;

namespace TecFlow.WebUi.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWebUiServices(this IServiceCollection services, IConfiguration configuration)
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
