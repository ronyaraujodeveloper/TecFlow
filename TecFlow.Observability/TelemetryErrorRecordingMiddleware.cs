using Microsoft.AspNetCore.Http;
using TecFlow.Business.Interfaces.Telemetry;

namespace TecFlow.Observability;

public class TelemetryErrorRecordingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ITelemetryRecentErrorRecorder _recorder;

    public TelemetryErrorRecordingMiddleware(
        RequestDelegate next,
        ITelemetryRecentErrorRecorder recorder)
    {
        _next = next;
        _recorder = recorder;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);

            if (context.Response.StatusCode >= 500)
            {
                _recorder.Record(
                    "HTTP",
                    $"Resposta {context.Response.StatusCode} em {context.Request.Method} {context.Request.Path}",
                    null);
            }
        }
        catch (Exception ex)
        {
            _recorder.Record(
                "HTTP",
                ex.Message,
                $"{context.Request.Method} {context.Request.Path}");
            throw;
        }
    }
}
