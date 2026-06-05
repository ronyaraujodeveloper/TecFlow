using Microsoft.Extensions.DependencyInjection;
using TecFlow.Business.Integrations.Orders;
using TecFlow.Business.Integrations.Webhooks;
using TecFlow.Infrastructure.Services.Integrations.Webhooks;

namespace TecFlow.Infrastructure.Services.Integrations.Orders;

public static class MarketplaceOrderRegistrationExtensions
{
    public static IServiceCollection AddTecFlowMarketplaceOrders(this IServiceCollection services)
    {
        services.AddSingleton<StockConcurrencyGate>();
        services.AddScoped<IMarketplaceWebhookSignatureVerifier, MarketplaceWebhookSignatureVerifier>();
        services.AddScoped<IMarketplaceStockService, MarketplaceStockService>();
        services.AddScoped<IMarketplaceOrderService, MarketplaceOrderService>();
        return services;
    }
}
