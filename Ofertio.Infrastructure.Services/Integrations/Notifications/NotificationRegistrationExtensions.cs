using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ofertio.Business.Integrations.Notifications;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Infrastructure.Services.Integrations.Notifications;

namespace Ofertio.Infrastructure.Services.Integrations;

public static class NotificationRegistrationExtensions
{
    public static IServiceCollection AddOfertioPushNotifications(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FirebaseOptions>(configuration.GetSection(FirebaseOptions.SectionName));
        services.AddScoped<INotificationHubService, NotificationHubService>();
        return services;
    }
}
