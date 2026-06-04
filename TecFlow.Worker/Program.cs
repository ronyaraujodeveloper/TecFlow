using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using TecFlow.Business.Service.Application;
using TecFlow.Infrastructure;
using TecFlow.Infrastructure.Services;
using TecFlow.Observability;

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
        services.AddTecFlowCoreServices();
        services.AddTecFlowInfrastructureServices(context.Configuration);
        services.AddTecFlowInfrastructureData(context.Configuration);
        services.AddTecFlowApplicationServices();
        services.AddTecFlowEngagementMessaging(context.Configuration, TecFlow.Infrastructure.Services.Messaging.TecFlowMessagingRole.Consumer);
        services.AddTecFlowTelemetry(context.Configuration, "TecFlow.Worker");
        services.AddHostedService<WorkerService>();
    })
    .Build();

await host.RunAsync();
