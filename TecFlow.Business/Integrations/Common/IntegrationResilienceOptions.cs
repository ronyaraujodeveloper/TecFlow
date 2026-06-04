namespace TecFlow.Business.Integrations.Common;

/// <summary>Políticas de resiliência compartilhadas entre integrações externas.</summary>
public class IntegrationResilienceOptions
{
    public const string SectionName = "Integrations:Resilience";

    public int RetryCount { get; set; } = 3;
    public int CircuitBreakerFailures { get; set; } = 5;
    public int CircuitBreakerDurationSeconds { get; set; } = 30;
}
