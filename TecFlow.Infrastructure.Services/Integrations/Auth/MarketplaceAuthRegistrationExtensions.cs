using Microsoft.Extensions.DependencyInjection;
using TecFlow.Business.Integrations.Auth;

namespace TecFlow.Infrastructure.Services.Integrations.Auth;

public static class MarketplaceAuthRegistrationExtensions
{
    public static IServiceCollection AddTecFlowMarketplaceAuth(this IServiceCollection services)
    {
        services.AddHttpClient("TecFlow.MarketplaceOAuth", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(100);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });

        services.AddScoped<IMarketplaceSignatureService, MarketplaceSignatureService>();
        services.AddScoped<IMarketplaceAuthService, MarketplaceAuthService>();

        return services;
    }
}
