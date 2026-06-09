using Microsoft.JSInterop;
using TecFlow.Database.Entity;
using TecFlow.SharedUi.Services.Integrations;
using TecFlow.SharedUi.Services.State;

namespace TecFlow.WebUi.Services.State;

public sealed class ActiveStoreScopeService : IActiveStoreScopeService
{
    private const string StorageKey = "tecflow.activeStoreId";

    private readonly IIntegracaoLojaApiService _integracaoApi;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<ActiveStoreScopeService> _logger;

    private List<IntegracaoLoja> _stores = [];
    private bool _initialized;
    private bool _browserRestored;

    public ActiveStoreScopeService(
        IIntegracaoLojaApiService integracaoApi,
        IJSRuntime jsRuntime,
        ILogger<ActiveStoreScopeService> logger)
    {
        _integracaoApi = integracaoApi;
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public IntegracaoLoja? ActiveStore { get; private set; }

    public int? ActiveStoreId => ActiveStore?.Id;

    public IReadOnlyList<IntegracaoLoja> Stores => _stores;

    public bool IsInitialized => _initialized;

    public bool IsLoading { get; private set; }

    public event Action? OnStoreChanged;

    public async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }

        await LoadStoresInternalAsync(cancellationToken);
        _initialized = true;
    }

    public async Task RestoreFromBrowserAsync(CancellationToken cancellationToken = default)
    {
        if (_browserRestored)
        {
            return;
        }

        _browserRestored = true;

        if (_stores.Count == 0)
        {
            return;
        }

        try
        {
            var storedId = await _jsRuntime.InvokeAsync<string?>(
                "tecflowActiveStore.get",
                cancellationToken);

            if (int.TryParse(storedId, out var lojaId))
            {
                var match = _stores.FirstOrDefault(s => s.Id == lojaId);
                if (match is not null)
                {
                    await ApplyActiveStoreAsync(match, persist: false, cancellationToken);
                    return;
                }
            }
        }
        catch (JSException ex)
        {
            _logger.LogDebug(ex, "LocalStorage indisponível para restaurar escopo de loja.");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug(ex, "Interop JS ainda não disponível para restaurar escopo de loja.");
        }

        if (ActiveStore is null && _stores.Count > 0)
        {
            await ApplyActiveStoreAsync(_stores[0], persist: true, cancellationToken);
        }
    }

    public async Task SetActiveStoreAsync(IntegracaoLoja store, CancellationToken cancellationToken = default)
    {
        if (store.Id <= 0)
        {
            return;
        }

        await ApplyActiveStoreAsync(store, persist: true, cancellationToken);
    }

    public async Task RefreshStoresAsync(CancellationToken cancellationToken = default)
    {
        var previousId = ActiveStore?.Id;
        await LoadStoresInternalAsync(cancellationToken);

        if (previousId.HasValue)
        {
            var match = _stores.FirstOrDefault(s => s.Id == previousId.Value);
            if (match is not null)
            {
                await ApplyActiveStoreAsync(match, persist: false, cancellationToken);
                return;
            }
        }

        if (ActiveStore is null && _stores.Count > 0)
        {
            await ApplyActiveStoreAsync(_stores[0], persist: true, cancellationToken);
        }
        else if (ActiveStore is not null && _stores.All(s => s.Id != ActiveStore.Id))
        {
            ActiveStore = null;
            await ClearPersistedStoreIdAsync(cancellationToken);
            OnStoreChanged?.Invoke();
        }
    }

    private async Task LoadStoresInternalAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;

        try
        {
            var response = await _integracaoApi.ListAsync(cancellationToken: cancellationToken);
            _stores = response.Status && response.DataList is { Count: > 0 }
                ? response.DataList.OrderByDescending(s => s.CreatedAt).ToList()
                : [];

            if (ActiveStore is not null)
            {
                ActiveStore = _stores.FirstOrDefault(s => s.Id == ActiveStore.Id);
            }

            if (ActiveStore is null && _stores.Count > 0 && _browserRestored)
            {
                await ApplyActiveStoreAsync(_stores[0], persist: true, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar lojas para escopo global.");
            _stores = [];
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ApplyActiveStoreAsync(
        IntegracaoLoja store,
        bool persist,
        CancellationToken cancellationToken)
    {
        if (ActiveStore?.Id == store.Id)
        {
            ActiveStore = store;
            return;
        }

        ActiveStore = store;

        if (persist)
        {
            await PersistStoreIdAsync(store.Id, cancellationToken);
        }

        OnStoreChanged?.Invoke();
    }

    private async Task PersistStoreIdAsync(int lojaId, CancellationToken cancellationToken)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("tecflowActiveStore.set", cancellationToken, lojaId.ToString());
        }
        catch (JSException ex)
        {
            _logger.LogDebug(ex, "Não foi possível persistir loja ativa no LocalStorage.");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug(ex, "Interop JS indisponível ao persistir loja ativa.");
        }
    }

    private async Task ClearPersistedStoreIdAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("tecflowActiveStore.clear", cancellationToken);
        }
        catch (JSException ex)
        {
            _logger.LogDebug(ex, "Não foi possível limpar loja ativa do LocalStorage.");
        }
        catch (InvalidOperationException)
        {
            // Ignorado durante prerender ou circuito indisponível.
        }
    }
}
