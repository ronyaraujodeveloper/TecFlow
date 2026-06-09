using TecFlow.Database.Entity;

namespace TecFlow.SharedUi.Services.State;

/// <summary>
/// Escopo global da loja/marketplace ativa na sessão Blazor (multi-contas).
/// </summary>
public interface IActiveStoreScopeService
{
    IntegracaoLoja? ActiveStore { get; }

    int? ActiveStoreId { get; }

    IReadOnlyList<IntegracaoLoja> Stores { get; }

    bool IsInitialized { get; }

    bool IsLoading { get; }

    event Action? OnStoreChanged;

    Task EnsureInitializedAsync(CancellationToken cancellationToken = default);

    Task RestoreFromBrowserAsync(CancellationToken cancellationToken = default);

    Task SetActiveStoreAsync(IntegracaoLoja store, CancellationToken cancellationToken = default);

    Task RefreshStoresAsync(CancellationToken cancellationToken = default);
}
