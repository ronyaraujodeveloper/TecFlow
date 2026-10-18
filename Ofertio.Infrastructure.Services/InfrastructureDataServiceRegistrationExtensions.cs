using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ofertio.Business.Interfaces.Repositories;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Database;
using Ofertio.Database.Data;
using Ofertio.Database.MultiTenancy;
using Ofertio.Infrastructure.Data;
using Ofertio.Infrastructure.Interfaces;
using Ofertio.Infrastructure.Security;
using Ofertio.Infrastructure.Services.Repositories;
using Ofertio.Infrastructure.Services.Security;
using Ofertio.Business.Domain.Sales;
using Ofertio.Business.Interfaces.Sales;
using Ofertio.Business.Interfaces.Inventory;
using Ofertio.Infrastructure.Services.Stock;
using Ofertio.Infrastructure.Services.Sales;
using Ofertio.Infrastructure.Services.Tenancy;
using Ofertio.Infrastructure.Services.Integrations;
using Microsoft.Extensions.Options;
using Ofertio.Business.Security;
using Ofertio.Util.Security;

namespace Ofertio.Infrastructure.Services
{
    public static class InfrastructureDataServiceRegistrationExtensions
    {
        public static IServiceCollection AddOfertioInfrastructureData(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

            var provider = configuration.GetValue<string>("Database:Provider") ?? "SqlServer";

            return AddOfertioInfrastructureDataInternal(services, connectionString, provider, configuration);
        }

        public static IServiceCollection AddOfertioInfrastructureData(
            this IServiceCollection services,
            string connectionString,
            string provider = "PostgreSQL")
        {
            throw new InvalidOperationException(
                "Use AddOfertioInfrastructureData(services, configuration) para registrar o provider e a criptografia.");
        }

        private static IServiceCollection AddOfertioInfrastructureDataInternal(
            IServiceCollection services,
            string connectionString,
            string provider,
            IConfiguration? configuration)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString), "Connection string for database cannot be null or empty.");
            }

            if (configuration is not null)
            {
                services.Configure<EncryptionOptions>(configuration.GetSection(EncryptionOptions.SectionName));
                services.Configure<WebhookSecurityOptions>(
                    configuration.GetSection(WebhookSecurityOptions.SectionName));
                var dataEncryption = new DataEncryptionService(
                    configuration[$"{EncryptionOptions.SectionName}:Key"] ?? string.Empty);
                services.AddSingleton<IDataEncryptionService>(dataEncryption);
                services.AddSingleton<IEncryptionService>(dataEncryption);
                services.AddOfertioDataProtection(configuration);
            }

            services.AddHttpContextAccessor();
            services.AddScoped<IUserContextProvider, UserContextProvider>();
            services.AddScoped<ICurrentTenantService, CurrentTenantService>();

            void ConfigureAppDbContext(DbContextOptionsBuilder options)
            {
                options.UseConfiguredProvider(connectionString, provider);

                var env = configuration?["ASPNETCORE_ENVIRONMENT"]
                    ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

                if (string.Equals(env, Environments.Development, StringComparison.OrdinalIgnoreCase))
                {
                    options.EnableSensitiveDataLogging();
                }
            }

            services.AddDbContextFactory<AppDbContext>(ConfigureAppDbContext, ServiceLifetime.Scoped);
            services.AddDbContext<AppDbContext>(ConfigureAppDbContext);

            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ICampaignRepository, CampaignRepository>();
            services.AddScoped<IContentRepository, ContentRepository>();
            services.AddScoped<IUserAccountRepository, UserAccountRepository>();
            services.AddScoped<IUserLoginRepository, UserLoginRepository>();
            services.AddScoped<IMarketplaceTokenRepository, MarketplaceTokenRepository>();
            services.AddScoped<IMarketplaceOrderRepository, MarketplaceOrderRepository>();
            services.AddScoped<IUserDeviceTokenRepository, UserDeviceTokenRepository>();
            services.AddScoped<IMarketplaceAccountRepository, MarketplaceAccountRepository>();
            services.AddScoped<IIntegracaoLojaRepository, IntegracaoLojaRepository>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<ISalesOrderRepository, SalesOrderRepository>();
            services.AddScoped<IOrderStateMachine, OrderStateMachine>();
            services.AddScoped<ISalesOrderService, SalesOrderService>();
            services.AddScoped<IInvoiceOrchestrator, InvoiceOrchestrator>();
            services.AddScoped<IInventoryRepository, InventoryRepository>();
            services.AddScoped<IInventoryService, InventoryService>();
            services.AddScoped<IInventoryAlertHook, LoggingInventoryAlertHook>();
            services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
            services.AddScoped<IAffiliateRepository, AffiliateRepository>();
            services.AddScoped<IMetricRepository, MetricRepository>();
            services.AddScoped<IDataService, DataService>();
            services.AddScoped<LegacyCredentialReEncryptService>();

            services.AddOfertioIdentity();

            services.AddScoped<IIntegracaoLojaService, IntegracaoLojaService>();
            services.AddScoped<MarketplaceAccountService>();

            return services;
        }
    }
}
