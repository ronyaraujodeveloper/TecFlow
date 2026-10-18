using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Ofertio.Business.Interfaces.Telemetry;
using Ofertio.Business.Telemetry;

namespace Ofertio.Observability;

public static class TelemetryRegistrationExtensions
{
    public static IServiceCollection AddOfertioTelemetry(
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
            services.AddSingleton<IOfertioBusinessMetrics, NoOpOfertioBusinessMetrics>();
            services.AddSingleton<ITelemetryRecentErrorRecorder, TelemetryRecentErrorRecorder>();
            return services;
        }

        var serviceName = serviceNameOverride ?? options.ServiceName;

        services.AddSingleton<OfertioBusinessMetrics>();
        services.AddSingleton<IOfertioBusinessMetrics>(sp => sp.GetRequiredService<OfertioBusinessMetrics>());
        services.AddSingleton<ITelemetryRecentErrorRecorder, TelemetryRecentErrorRecorder>();

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName, serviceVersion: options.ServiceVersion)
                .AddTelemetrySdk())
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(OfertioActivitySources.EngagementSourceName)
                    .AddSource(OfertioActivitySources.MarketplaceSourceName)
                    .AddSource(OfertioActivitySources.ConciliationSourceName)
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
                    .AddMeter(OfertioBusinessMetrics.MeterName)
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

                if (options.EnablePrometheusEndpoint)
                {
                    metrics.AddPrometheusExporter();
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

internal sealed class NoOpOfertioBusinessMetrics : IOfertioBusinessMetrics
{
    public long ComentariosProcessadosTotal => 0;
    public long LinksEnviadosSucessoTotal => 0;
    public long ErrosConciliacaoContagem => 0;
    public void RecordCommentProcessed(bool success) { }
    public void RecordAffiliateLinkSent(bool success) { }
    public void RecordConciliationError() { }
}
