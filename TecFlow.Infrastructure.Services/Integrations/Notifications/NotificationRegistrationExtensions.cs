using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TecFlow.Business.Integrations.Notifications;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Infrastructure.Services.Integrations.Notifications;

namespace TecFlow.Infrastructure.Services.Integrations;

public static class NotificationRegistrationExtensions
{
    public static IServiceCollection AddTecFlowPushNotifications(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FirebaseOptions>(configuration.GetSection(FirebaseOptions.SectionName));
        services.AddScoped<INotificationHubService, NotificationHubService>();
        return services;
    }
}
