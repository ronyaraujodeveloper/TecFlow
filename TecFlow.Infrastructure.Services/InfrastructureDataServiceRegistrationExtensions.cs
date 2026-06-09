using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Database;
using TecFlow.Database.Data;
using TecFlow.Database.MultiTenancy;
using TecFlow.Infrastructure.Data;
using TecFlow.Infrastructure.Security;
using TecFlow.Infrastructure.Services.Repositories;
using TecFlow.Infrastructure.Services.Security;
using TecFlow.Business.Domain.Sales;
using TecFlow.Business.Interfaces.Sales;
using TecFlow.Business.Interfaces.Inventory;
using TecFlow.Infrastructure.Services.Stock;
using TecFlow.Infrastructure.Services.Sales;
using TecFlow.Infrastructure.Services.Tenancy;
using TecFlow.Infrastructure.Services.Integrations;
using TecFlow.Util.Security;

namespace TecFlow.Infrastructure.Services
{
    public static class InfrastructureDataServiceRegistrationExtensions
    {
        public static IServiceCollection AddTecFlowInfrastructureData(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = PostgreSqlConnectionStringExtensions.EnsureUtf8Encoding(
                configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured."));

            var provider = configuration.GetValue<string>("Database:Provider") ?? "PostgreSQL";

            return AddTecFlowInfrastructureDataInternal(services, connectionString, provider, configuration);
        }

        public static IServiceCollection AddTecFlowInfrastructureData(
            this IServiceCollection services,
            string connectionString,
            string provider = "PostgreSQL")
        {
            throw new InvalidOperationException(
                "Use AddTecFlowInfrastructureData(services, configuration) para registrar PostgreSQL e criptografia.");
        }

        private static IServiceCollection AddTecFlowInfrastructureDataInternal(
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
                services.AddTecFlowEncryption(configuration);
            }

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentTenantService, CurrentTenantService>();

            services.AddDbContext<AppDbContext>(options =>
            {
                if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
                {
                    throw new NotSupportedException(
                        "SqlServer foi descontinuado neste projeto. Configure Database:Provider=PostgreSQL.");
                }

                options.AddInterceptors(new NpgsqlUtf8ClientEncodingInterceptor());
                options.UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsAssembly("TecFlow.Infrastructure"));

                var env = configuration?["ASPNETCORE_ENVIRONMENT"]
                    ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

                if (string.Equals(env, Environments.Development, StringComparison.OrdinalIgnoreCase))
                {
                    options.EnableSensitiveDataLogging();
                }
            });

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

            services.AddTecFlowIdentity();

            services.AddScoped<IIntegracaoLojaService, IntegracaoLojaService>();

            return services;
        }
    }
}
