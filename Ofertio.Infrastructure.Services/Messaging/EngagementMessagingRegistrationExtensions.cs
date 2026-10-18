using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ofertio.Business.Interfaces.Messaging;
using Ofertio.Business.Messaging;
using Ofertio.Infrastructure.Services.Messaging.Consumers;

namespace Ofertio.Infrastructure.Services.Messaging;

public enum OfertioMessagingRole
{
    Publisher,
    Consumer
}

public static class EngagementMessagingRegistrationExtensions
{
    public static IServiceCollection AddOfertioEngagementMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        OfertioMessagingRole role)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.Configure<EngagementKeywordTriageOptions>(
            configuration.GetSection(EngagementKeywordTriageOptions.SectionName));
        services.AddSingleton<ICommentKeywordTriageService, CommentKeywordTriageService>();
        services.AddScoped<IAffiliateLinkDeliveryNotifier, AffiliateLinkDeliveryNotifier>();

        var rabbitOptions = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>()
            ?? new RabbitMqOptions();

        if (!rabbitOptions.Enabled)
        {
            services.AddSingleton<IEngagementEventPublisher, NoOpEngagementEventPublisher>();
            return services;
        }

        services.AddMassTransit(x =>
        {
            if (role == OfertioMessagingRole.Consumer)
            {
                x.AddConsumer<SocialMediaCommentConsumer>();
            }

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitOptions.Host, rabbitOptions.Port, rabbitOptions.VirtualHost, h =>
                {
                    h.Username(rabbitOptions.Username);
                    h.Password(rabbitOptions.Password);
                });

                if (role == OfertioMessagingRole.Consumer)
                {
                    cfg.ReceiveEndpoint(rabbitOptions.CommentQueueName, e =>
                    {
                        e.ConfigureConsumeTopology = true;
                        e.UseMessageRetry(r => r.Interval(
                            rabbitOptions.RetryCount,
                            TimeSpan.FromSeconds(rabbitOptions.RetryIntervalSeconds)));
                        e.BindDeadLetterQueue(rabbitOptions.CommentDeadLetterQueueName);
                        e.ConfigureConsumer<SocialMediaCommentConsumer>(context);
                    });
                }
            });
        });

        services.AddScoped<IEngagementEventPublisher, MassTransitEngagementEventPublisher>();
        return services;
    }
}
