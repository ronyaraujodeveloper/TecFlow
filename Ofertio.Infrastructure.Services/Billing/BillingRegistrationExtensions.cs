using Microsoft.Extensions.DependencyInjection;
using Ofertio.Business.Interfaces.Services;

namespace Ofertio.Infrastructure.Services.Billing;

public static class BillingRegistrationExtensions
{
    /// <summary>Meus dados, Meu plano e pagamento. Troque o gateway simulado pelo real na fase de cobrança.</summary>
    public static IServiceCollection AddOfertioBilling(this IServiceCollection services)
    {
        services.AddScoped<UserProfileService>();
        services.AddScoped<IUserProfileService>(sp => sp.GetRequiredService<UserProfileService>());
        services.AddScoped<IMyPlanService, MyPlanService>();
        services.AddSingleton<IPlanPaymentGateway, SimulatedPlanPaymentGateway>();
        return services;
    }
}
