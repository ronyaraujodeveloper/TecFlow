namespace TecFlow.Business.Telemetry;

public class TelemetryOptions
{
    public const string SectionName = "Telemetry";

    public bool Enabled { get; set; } = true;
    public string ServiceName { get; set; } = "TecFlow";
    public string ServiceVersion { get; set; } = "1.0.0";
    public string? OtlpEndpoint { get; set; }
    public string? SeqServerUrl { get; set; }
    public bool EnableConsoleExporter { get; set; } = true;
    public bool EnablePrometheusEndpoint { get; set; } = true;
    public string PrometheusEndpointPath { get; set; } = "/metrics";
}
