using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Database;
using TecFlow.Infrastructure.Data;
using TecFlow.Infrastructure.Services.Repositories;
using TecFlow.Infrastructure.Services.Security;
using TecFlow.Util.Security;

namespace TecFlow.Infrastructure.Services
{
    public static class InfrastructureDataServiceRegistrationExtensions
    {
        public static IServiceCollection AddTecFlowInfrastructureData(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

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

            services.AddDbContext<AppDbContext>(options =>
            {
                if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
                {
                    throw new NotSupportedException(
                        "SqlServer foi descontinuado neste projeto. Configure Database:Provider=PostgreSQL.");
                }

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
            services.AddScoped<IMarketplaceTokenRepository, MarketplaceTokenRepository>();
            services.AddScoped<IMarketplaceOrderRepository, MarketplaceOrderRepository>();
            services.AddScoped<IUserDeviceTokenRepository, UserDeviceTokenRepository>();
            services.AddScoped<IAffiliateRepository, AffiliateRepository>();
            services.AddScoped<IMetricRepository, MetricRepository>();
            services.AddScoped<IDataService, DataService>();
            services.AddScoped<LegacyCredentialReEncryptService>();

            return services;
        }
    }
}
