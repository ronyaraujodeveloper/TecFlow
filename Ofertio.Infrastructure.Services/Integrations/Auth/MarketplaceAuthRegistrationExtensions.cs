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

        services.AddScoped<IMarketplaceSignatureService, MarketplaceSignatureService>();
        services.AddScoped<IMarketplaceAuthService, MarketplaceAuthService>();
        services.AddSingleton<IOAuthStateProtector, OAuthStateProtector>();

        return services;
    }
}
