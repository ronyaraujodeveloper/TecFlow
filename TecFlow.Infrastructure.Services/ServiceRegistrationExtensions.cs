using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TecFlow.Infrastructure.Interfaces;
using TecFlow.Infrastructure.Security;
using TecFlow.Infrastructure.Services.Integrations;
using TecFlow.Infrastructure.Services.Integrations.Auth;
using TecFlow.Infrastructure.Services.Integrations.Catalog;
using TecFlow.Infrastructure.Services.Integrations.Orders;


namespace TecFlow.Infrastructure.Services
{
    public static class ServiceRegistrationExtensions
    {
        public static IServiceCollection AddTecFlowInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // DbContext e AppConfiguration registros...
            services.AddSingleton<IAppConfiguration>(sp => {
                var logger = sp.GetRequiredService<ILogger<AppConfiguration>>();
                return new AppConfiguration(configuration, logger);
            });

            services.AddScoped<JwtTokenService>();

            services.AddTecFlowIntegrationHttpClients(configuration);
            services.AddTecFlowMarketplaceAuth();
            services.AddTecFlowMarketplaceCatalog();
            services.AddTecFlowMarketplaceOrders();
            services.AddTecFlowExternalServices();
            services.AddTecFlowPushNotifications(configuration);

            return services;
        }
    }
}