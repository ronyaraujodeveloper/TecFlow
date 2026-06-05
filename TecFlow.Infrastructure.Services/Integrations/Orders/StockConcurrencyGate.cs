using System.Collections.Concurrent;

namespace TecFlow.Infrastructure.Services.Integrations.Orders;

/// <summary>Serializa ajustes de estoque por chave loja+SKU para evitar condições de corrida.</summary>
public sealed class StockConcurrencyGate
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public async Task<T> RunAsync<T>(string key, Func<Task<T>> action)
    {
        var gate = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            return await action();
        }
        finally
        {
            gate.Release();
        }
    }
}
