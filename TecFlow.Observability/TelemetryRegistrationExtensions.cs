using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TecFlow.Business.Interfaces.Telemetry;
using TecFlow.Business.Telemetry;

namespace TecFlow.Observability;

public static class TelemetryRegistrationExtensions
{
    public static IServiceCollection AddTecFlowTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        string? serviceNameOverride = null,
        bool enableAspNetCoreInstrumentation = false)
    {
        services.Configure<TelemetryOptions>(configuration.GetSection(TelemetryOptions.SectionName));

        var options = configuration.GetSection(TelemetryOptions.SectionName).Get<TelemetryOptions>()
            ?? new TelemetryOptions();

        if (!options.Enabled)
        {
            services.AddSingleton<ITecFlowBusinessMetrics, NoOpTecFlowBusinessMetrics>();
            services.AddSingleton<ITelemetryRecentErrorRecorder, TelemetryRecentErrorRecorder>();
            return services;
        }

        var serviceName = serviceNameOverride ?? options.ServiceName;

        services.AddSingleton<TecFlowBusinessMetrics>();
        services.AddSingleton<ITecFlowBusinessMetrics>(sp => sp.GetRequiredService<TecFlowBusinessMetrics>());
        services.AddSingleton<ITelemetryRecentErrorRecorder, TelemetryRecentErrorRecorder>();

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName, serviceVersion: options.ServiceVersion)
                .AddTelemetrySdk())
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(TecFlowActivitySources.EngagementSourceName)
                    .AddSource(TecFlowActivitySources.MarketplaceSourceName)
                    .AddSource(TecFlowActivitySources.ConciliationSourceName)
                    .AddHttpClientInstrumentation(o => o.RecordException = true);

                if (enableAspNetCoreInstrumentation)
                {
                    tracing.AddAspNetCoreInstrumentation(o =>
                    {
                        o.RecordException = true;
                        o.Filter = ctx =>
                            !ctx.Request.Path.StartsWithSegments("/metrics", StringComparison.OrdinalIgnoreCase);
                    });
                }

                if (options.EnableConsoleExporter)
                {
                    tracing.AddConsoleExporter();
                }

                if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint))
                {
                    tracing.AddOtlpExporter(exp => exp.Endpoint = new Uri(options.OtlpEndpoint));
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(TecFlowBusinessMetrics.MeterName)
                    .AddRuntimeInstrumentation()
                    .AddHttpClientInstrumentation();

                if (enableAspNetCoreInstrumentation)
                {
                    metrics.AddAspNetCoreInstrumentation();
                }

                if (options.EnableConsoleExporter)
                {
                    metrics.AddConsoleExporter();
                }

                if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint))
                {
                    metrics.AddOtlpExporter(exp => exp.Endpoint = new Uri(options.OtlpEndpoint));
                }
            });

        services.AddLogging(logging =>
        {
            logging.AddOpenTelemetry(otel =>
            {
                otel.IncludeFormattedMessage = true;
                otel.IncludeScopes = true;
                otel.ParseStateValues = true;
                otel.SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService(serviceName, serviceVersion: options.ServiceVersion));

                if (options.EnableConsoleExporter)
                {
                    otel.AddConsoleExporter();
                }

                if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint))
                {
                    otel.AddOtlpExporter(exp => exp.Endpoint = new Uri(options.OtlpEndpoint));
                }
            });
        });

        return services;
    }
}

internal sealed class NoOpTecFlowBusinessMetrics : ITecFlowBusinessMetrics
{
    public long ComentariosProcessadosTotal => 0;
    public long LinksEnviadosSucessoTotal => 0;
    public long ErrosConciliacaoContagem => 0;
    public void RecordCommentProcessed(bool success) { }
    public void RecordAffiliateLinkSent(bool success) { }
    public void RecordConciliationError() { }
}
