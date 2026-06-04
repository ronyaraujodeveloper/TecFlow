using Polly;
using Polly.Extensions.Http;
using TecFlow.Business.Integrations.Common;

namespace TecFlow.Infrastructure.Services.Integrations.Common;

public static class IntegrationResiliencePolicies
{
    public static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy(IntegrationResilienceOptions options) =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                options.RetryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

    public static IAsyncPolicy<HttpResponseMessage> CreateCircuitBreakerPolicy(IntegrationResilienceOptions options) =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                options.CircuitBreakerFailures,
                TimeSpan.FromSeconds(options.CircuitBreakerDurationSeconds));
}
