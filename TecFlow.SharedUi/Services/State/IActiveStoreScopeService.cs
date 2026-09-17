using TecFlow.Business.Dto;

namespace TecFlow.SharedUi.Services.State;

/// <summary>
/// Escopo global da loja/marketplace ativa na sessão Blazor (multi-contas).
/// </summary>
public interface IActiveStoreScopeService
{
    MarketplaceAccountDto? ActiveStore { get; }

    int? ActiveStoreId { get; }

    IReadOnlyList<MarketplaceAccountDto> Stores { get; }

    bool IsInitialized { get; }

    bool IsLoading { get; }

    event Action? OnStoreChanged;

    Task EnsureInitializedAsync(CancellationToken cancellationToken = default);

    Task RestoreFromBrowserAsync(CancellationToken cancellationToken = default);

    Task SetActiveStoreAsync(MarketplaceAccountDto store, CancellationToken cancellationToken = default);

    Task RefreshStoresAsync(CancellationToken cancellationToken = default);
}
