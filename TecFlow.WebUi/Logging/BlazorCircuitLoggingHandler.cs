using Microsoft.AspNetCore.Components.Server.Circuits;

namespace TecFlow.WebUi.Logging;

/// <summary>
/// Registra abertura, fechamento e reconexão de circuitos Blazor Server (SignalR).
/// </summary>
public sealed class BlazorCircuitLoggingHandler : CircuitHandler
{
    private readonly ILogger<BlazorCircuitLoggingHandler> _logger;

    public BlazorCircuitLoggingHandler(ILogger<BlazorCircuitLoggingHandler> logger)
    {
        _logger = logger;
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Blazor circuit aberto: {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Blazor circuit fechado: {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Conexao SignalR perdida no circuito {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Conexao SignalR restabelecida no circuito {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }
}
