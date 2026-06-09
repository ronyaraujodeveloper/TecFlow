using Microsoft.Extensions.DependencyInjection;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Application;

namespace TecFlow.Business.Service.LinkStrategies;
public static class LinkStrategyServiceCollectionExtensions
{
    public static IServiceCollection AddAffiliateLinkStrategyServices(this IServiceCollection services)
    {
        services.AddScoped<IPlatformLinkStrategy, ShopeeLinkStrategy>();
        services.AddScoped<IPlatformLinkStrategy, TikTokLinkStrategy>();
        services.AddScoped<PlatformLinkResolver>();
        services.AddScoped<IAffiliateLinkGenerationContext, AffiliateLinkGenerationContext>();
        services.AddScoped<IAffiliateLinkGenerationService, AffiliateLinkGenerationService>();

        return services;
    }
}
