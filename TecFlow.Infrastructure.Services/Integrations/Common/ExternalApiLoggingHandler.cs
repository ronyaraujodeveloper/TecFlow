using Microsoft.Extensions.Logging;

namespace TecFlow.Infrastructure.Services.Integrations.Common;

/// <summary>Registra método, URL, status e duração das chamadas HTTP externas (sem corpo/senhas).</summary>
public sealed class ExternalApiLoggingHandler : DelegatingHandler
{
    private readonly ILogger<ExternalApiLoggingHandler> _logger;
    private readonly string _platformName;

    public ExternalApiLoggingHandler(ILogger<ExternalApiLoggingHandler> logger, string platformName)
    {
        _logger = logger;
        _platformName = platformName;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var started = DateTime.UtcNow;

        _logger.LogInformation(
            "[{Platform}] HTTP {Method} {Url}",
            _platformName,
            request.Method,
            request.RequestUri);

        var response = await base.SendAsync(request, cancellationToken);

        var elapsedMs = (DateTime.UtcNow - started).TotalMilliseconds;

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation(
                "[{Platform}] HTTP {StatusCode} em {ElapsedMs:F0}ms — {Method} {Url}",
                _platformName,
                (int)response.StatusCode,
                elapsedMs,
                request.Method,
                request.RequestUri);
        }
        else
        {
            _logger.LogWarning(
                "[{Platform}] HTTP {StatusCode} em {ElapsedMs:F0}ms — {Method} {Url}",
                _platformName,
                (int)response.StatusCode,
                elapsedMs,
                request.Method,
                request.RequestUri);
        }

        return response;
    }
}
