using Microsoft.Extensions.DependencyInjection;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Infrastructure.Services.Service;

using Ofertio.Infrastructure.Services.Service.ExternalServices;
using Ofertio.Infrastructure.Services.Services; // Se necessário

namespace Ofertio.Infrastructure
{
    public static class CoreServiceRegistrationExtensions
    {
        public static IServiceCollection AddOfertioCoreServices(this IServiceCollection services)
        {
            services.AddScoped<IAnaliseCalculoService, AnaliseCalculoService>();

            services.AddScoped<IScoreService, ScoreService>();
            services.AddScoped<IAnaliseService, AnaliseService>();

            return services;
        }
    }
}