using Microsoft.Extensions.DependencyInjection;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Infrastructure.Services.Service;

using TecFlow.Infrastructure.Services.Service.ExternalServices;
using TecFlow.Infrastructure.Services.Services; // Se necessário

namespace TecFlow.Infrastructure
{
    public static class CoreServiceRegistrationExtensions
    {
        public static IServiceCollection AddTecFlowCoreServices(this IServiceCollection services)
        {
            services.AddScoped<IAnaliseCalculoService, AnaliseCalculoService>();

            services.AddScoped<IScoreService, ScoreService>();
            services.AddScoped<IAnaliseService, AnaliseService>();

            return services;
        }
    }
}