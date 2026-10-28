using Microsoft.Extensions.DependencyInjection;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Infrastructure.Services.Billing;
using Ofertio.Infrastructure.Services.Groups;

namespace Ofertio.Infrastructure.Services.Master;

public static class MasterBackofficeRegistrationExtensions
{
    public static IServiceCollection AddOfertioMasterBackoffice(this IServiceCollection services)
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
        services.AddOfertioUploadStorage();
        services.AddScoped<ITenantAdminService, TenantAdminService>();
        services.AddScoped<IImpersonationService, ImpersonationService>();
        services.AddScoped<IMasterMetricsService, MasterMetricsService>();
        services.AddScoped<ISystemCommunicationService, SystemCommunicationService>();
        services.AddScoped<IAsaasWebhookService, AsaasWebhookService>();
        services.AddHttpClient<ISaasPaymentGateway, AsaasPaymentGateway>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(20);
        });
        services.AddOfertioBilling();
        services.AddScoped<IMenuPermissionService, MenuPermissionService>();
        services.AddScoped<IGlobalIntegrationService, GlobalIntegrationService>();
        services.AddScoped<IDeveloperAccountService, DeveloperAccountService>();
        services.AddScoped<IGlobalCredentialHealthService, GlobalCredentialHealthService>();
        services.AddHttpClient<IDeveloperAccountHealthChecker, DeveloperAccountHealthChecker>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Ofertio-HealthCheck/1.0");
        });
        return services;
    }
}
