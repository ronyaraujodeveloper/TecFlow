using TecFlow.Business.Dto;

namespace TecFlow.SharedUi.Services.State;

/// <summary>Implementação neutra para hosts sem seletor de escopo (ex.: Mobile).</summary>
public sealed class NullActiveStoreScopeService : IActiveStoreScopeService
{
    public MarketplaceAccountDto? ActiveStore => null;

    public int? ActiveStoreId => null;

    public IReadOnlyList<MarketplaceAccountDto> Stores { get; } = Array.Empty<MarketplaceAccountDto>();

    public bool IsInitialized => true;

    public bool IsLoading => false;

    public event Action? OnStoreChanged { add { } remove { } }

    public Task EnsureInitializedAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task RestoreFromBrowserAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task SetActiveStoreAsync(MarketplaceAccountDto store, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task RefreshStoresAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
