using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TecFlow.Infrastructure.Interfaces;
using TecFlow.Infrastructure.Security;


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

            services.AddTecFlowExternalServices();

            return services;
        }
    }
}