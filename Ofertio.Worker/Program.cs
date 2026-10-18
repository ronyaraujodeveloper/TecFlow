using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Ofertio.Business.Service.Application;
using Ofertio.Infrastructure;
using Ofertio.Infrastructure.Services;
using Ofertio.Observability;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((_, configurationBuilder) =>
    {
        DatabaseUrlConfiguration.ConfigureAppConfiguration(configurationBuilder);
    })
    .UseSerilog((context, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext())
    .ConfigureServices((context, services) =>
    {
        services.AddOfertioCoreServices();
        services.AddOfertioInfrastructureServices(context.Configuration);
        services.AddOfertioInfrastructureData(context.Configuration);
        services.AddOfertioApplicationServices();
        services.AddOfertioEngagementMessaging(context.Configuration, Ofertio.Infrastructure.Services.Messaging.OfertioMessagingRole.Consumer);
        services.AddOfertioTelemetry(context.Configuration, "Ofertio.Worker");
        services.AddHostedService<WorkerService>();
    })
    .Build();

await host.RunAsync();
