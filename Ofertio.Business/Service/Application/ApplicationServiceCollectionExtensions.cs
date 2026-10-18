using Microsoft.Extensions.DependencyInjection;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Pipelines;

namespace Ofertio.Business.Service.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddOfertioApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ColetaDadosPipeline>();
        services.AddScoped<GeracaoConteudoPipeline>();
        services.AddScoped<PublicacaoPipeline>();

        services.AddScoped<PromocoesApplicationService>();
        services.AddScoped<ProductsApplicationService>();
        services.AddScoped<CampaignsApplicationService>();
        services.AddScoped<PublicacaoApplicationService>();
        services.AddScoped<GeminiApplicationService>();
        services.AddScoped<AnaliseApplicationService>();
        services.AddScoped<AIApplicationService>();
        services.AddScoped<MetricsApplicationService>();
        services.AddScoped<ConfiguracaoApplicationService>();

        services.AddScoped<IOrquestradorService, OrquestradorApplicationService>();

        return services;
    }
}
