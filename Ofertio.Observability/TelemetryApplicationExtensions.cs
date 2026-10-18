using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using Ofertio.Business.Telemetry;

namespace Ofertio.Observability;

public static class TelemetryApplicationExtensions
{
    public static WebApplication UseOfertioTelemetry(this WebApplication app)
    {
        var options = app.Services.GetService<IOptions<TelemetryOptions>>()?.Value ?? new TelemetryOptions();
        if (!options.Enabled)
        {
            return app;
        }

        app.UseMiddleware<TelemetryErrorRecordingMiddleware>();

        if (options.EnablePrometheusEndpoint)
        {
            // Requer AddPrometheusExporter() em AddOfertioTelemetry → WithMetrics.
            app.UseOpenTelemetryPrometheusScrapingEndpoint(options.PrometheusEndpointPath);
        }

        return app;
    }
}
