using Microsoft.Extensions.DependencyInjection;
using Ofertio.Business.Integrations.Catalog;

namespace Ofertio.Infrastructure.Services.Integrations.Catalog;

public static class MarketplaceProductRegistrationExtensions
{
    public static IServiceCollection AddOfertioMarketplaceCatalog(this IServiceCollection services)
    {
        services.AddScoped<IMarketplaceProductService, MarketplaceProductService>();
        return services;
    }
}
