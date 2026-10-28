using Microsoft.Extensions.DependencyInjection;
using Ofertio.Business.Integrations.Auth;

namespace Ofertio.Infrastructure.Services.Integrations.Auth;

public static class MarketplaceAuthRegistrationExtensions
{
    public static IServiceCollection AddOfertioMarketplaceAuth(this IServiceCollection services)
    {
        services.AddHttpClient("Ofertio.MarketplaceOAuth", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(100);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });

        // A troca oficial do TikTok Shop leva app_secret na query: sem loggers do HttpClient, a URL nunca vai para o log.
        services.AddHttpClient(MarketplaceAuthService.TikTokShopOAuthClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(100);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }).RemoveAllLoggers();

        services.AddScoped<IMarketplaceSignatureService, MarketplaceSignatureService>();
        services.AddScoped<IMarketplaceAuthService, MarketplaceAuthService>();
        services.AddSingleton<IOAuthStateProtector, OAuthStateProtector>();

        return services;
    }
}
