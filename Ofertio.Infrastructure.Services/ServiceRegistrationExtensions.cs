using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ofertio.Infrastructure.Interfaces;
using Ofertio.Infrastructure.Security;
using Ofertio.Infrastructure.Services.Integrations;
using Ofertio.Infrastructure.Services.Integrations.Auth;
using Ofertio.Infrastructure.Services.Integrations.Catalog;
using Ofertio.Infrastructure.Services.Integrations.Orders;
using Ofertio.Infrastructure.Services.Messaging;
using Ofertio.Infrastructure.Services.ShortLinks;
using Ofertio.Infrastructure.Services.WhatsApp;


namespace Ofertio.Infrastructure.Services
{
    public static class ServiceRegistrationExtensions
    {
        public static IServiceCollection AddOfertioInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // DbContext e AppConfiguration registros...
            services.AddSingleton<IAppConfiguration>(sp => {
                var logger = sp.GetRequiredService<ILogger<AppConfiguration>>();
                return new AppConfiguration(configuration, logger);
            });

            services.AddScoped<JwtTokenService>();

            services.AddOfertioIntegrationHttpClients(configuration);
            services.AddOfertioMarketplaceAuth();
            services.AddOfertioMarketplaceCatalog();
            services.AddOfertioMarketplaceOrders();
            services.AddOfertioExternalServices();
            services.AddOfertioPushNotifications(configuration);
            services.AddOfertioShortLinkServices(configuration);
            services.AddOfertioWhatsAppEvolution(configuration);

            return services;
        }

        /// <summary>Registra MassTransit/RabbitMQ para publicação ou consumo de engajamento.</summary>
        public static IServiceCollection AddOfertioEngagementMessaging(
            this IServiceCollection services,
            IConfiguration configuration,
            OfertioMessagingRole role) =>
            EngagementMessagingRegistrationExtensions.AddOfertioEngagementMessaging(services, configuration, role);
    }
}