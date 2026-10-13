using Microsoft.Extensions.DependencyInjection;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.Infrastructure.Services.Master;

public static class MasterBackofficeRegistrationExtensions
{
    public static IServiceCollection AddTecFlowMasterBackoffice(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<IApiRequestCounter, ApiRequestCounter>();
        services.AddSingleton<ITenantAccessReader, TenantAccessReader>();
        services.AddScoped<IMasterAuditService, MasterAuditService>();
        services.AddScoped<IMasterOperatorService, MasterOperatorService>();
        services.AddScoped<ISystemSettingsService, SystemSettingsService>();
        services.AddScoped<SaasPlanService>();
        services.AddScoped<ISaasPlanService>(sp => sp.GetRequiredService<SaasPlanService>());
        services.AddScoped<IPlanLimitsService>(sp => sp.GetRequiredService<SaasPlanService>());
        services.AddScoped<ITenantAdminService, TenantAdminService>();
        services.AddScoped<IImpersonationService, ImpersonationService>();
        services.AddScoped<IMasterMetricsService, MasterMetricsService>();
        services.AddScoped<ISystemCommunicationService, SystemCommunicationService>();
        services.AddScoped<IAsaasWebhookService, AsaasWebhookService>();
        services.AddHttpClient<ISaasPaymentGateway, AsaasPaymentGateway>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(20);
        });
        return services;
    }
}
