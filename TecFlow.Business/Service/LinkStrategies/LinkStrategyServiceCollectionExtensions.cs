using Microsoft.Extensions.DependencyInjection;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Application;

namespace TecFlow.Business.Service.LinkStrategies;
public static class LinkStrategyServiceCollectionExtensions
{
    public static IServiceCollection AddAffiliateLinkStrategyServices(this IServiceCollection services)
    {
        services.AddScoped<IPlatformLinkStrategy, ShopeeLinkStrategy>();
        services.AddScoped<IPlatformLinkStrategy, TikTokShopLinkStrategy>();
        services.AddScoped<IPlatformLinkStrategy, MercadoLivreLinkStrategy>();
        services.AddScoped<IPlatformLinkStrategy, AmazonLinkStrategy>();
        services.AddScoped<IPlatformLinkStrategy, MagazineLuizaLinkStrategy>();
        services.AddScoped<IPlatformLinkStrategy, KabumLinkStrategy>();
        services.AddScoped<IPlatformLinkStrategy, CasasBahiaLinkStrategy>();
        services.AddScoped<PlatformLinkResolver>();
        services.AddScoped<IAffiliateLinkGenerationContext, AffiliateLinkGenerationContext>();
        services.AddScoped<IAffiliateLinkGenerationService, AffiliateLinkGenerationService>();

        return services;
    }
}
