using Microsoft.Extensions.DependencyInjection;
using TecFlow.Business.Integrations.Catalog;

namespace TecFlow.Infrastructure.Services.Integrations.Catalog;

public static class MarketplaceProductRegistrationExtensions
{
    public static IServiceCollection AddTecFlowMarketplaceCatalog(this IServiceCollection services)
    {
        services.AddScoped<IMarketplaceProductService, MarketplaceProductService>();
        return services;
    }
}
