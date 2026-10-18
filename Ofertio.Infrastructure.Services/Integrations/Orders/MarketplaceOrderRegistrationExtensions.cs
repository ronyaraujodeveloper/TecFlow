using Microsoft.Extensions.DependencyInjection;
using Ofertio.Business.Integrations.Orders;
using Ofertio.Business.Integrations.Webhooks;
using Ofertio.Infrastructure.Services.Integrations.Webhooks;

namespace Ofertio.Infrastructure.Services.Integrations.Orders;

public static class MarketplaceOrderRegistrationExtensions
{
    public static IServiceCollection AddOfertioMarketplaceOrders(this IServiceCollection services)
    {
        services.AddSingleton<StockConcurrencyGate>();
        services.AddScoped<IMarketplaceWebhookSignatureVerifier, MarketplaceWebhookSignatureVerifier>();
        services.AddScoped<IMarketplaceStockService, MarketplaceStockService>();
        services.AddScoped<IMarketplaceOrderService, MarketplaceOrderService>();
        return services;
    }
}
