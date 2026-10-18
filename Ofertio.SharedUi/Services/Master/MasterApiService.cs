using Ofertio.Business.Dto;
using Ofertio.SharedUi.Models;
using Ofertio.SharedUi.Services.Http;
using Ofertio.SharedUi.Services.UI;

namespace Ofertio.SharedUi.Services.Master;

public interface IMasterApiService
{
    Task<ApiResult<MasterDashboardDto>> DashboardAsync(CancellationToken cancellationToken = default);
    Task<ApiResult<List<TenantSummaryDto>>> TenantsAsync(TenantQueryDto query, CancellationToken cancellationToken = default);
    Task<ApiResult<MasterOperationResultDto>> TenantLifecycleAsync(Guid tenantId, TenantLifecycleRequestDto request, CancellationToken cancellationToken = default);
    Task<ApiResult<MasterOperationResultDto>> TenantPlanAsync(Guid tenantId, TenantPlanChangeDto request, CancellationToken cancellationToken = default);
    Task<ApiResult<MasterOperationResultDto>> TenantCreditsAsync(Guid tenantId, TenantCreditAdjustDto request, CancellationToken cancellationToken = default);
    Task<ApiResult<TenantStorageDto>> TenantStorageAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<ApiResult<MasterOperationResultDto>> TenantPurgeAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<ApiResult<MasterOperationResultDto>> TenantSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<ApiResult<List<MasterOperatorDto>>> OperatorsAsync(CancellationToken cancellationToken = default);
    Task<ApiResult<MasterOperatorDto>> SaveOperatorAsync(int? id, MasterOperatorSaveDto request, CancellationToken cancellationToken = default);
    Task<ApiResult<MasterOperationResultDto>> SetOperatorActiveAsync(int id, bool active, CancellationToken cancellationToken = default);
    Task<ApiResult<MasterPermissionMatrixDto>> PermissionsAsync(CancellationToken cancellationToken = default);
    Task<ApiResult<List<MasterAuditLogDto>>> AuditAsync(MasterAuditQueryDto query, CancellationToken cancellationToken = default);
    Task<ApiResult<List<SystemSettingItemDto>>> SettingsAsync(CancellationToken cancellationToken = default);
    Task<ApiResult<MasterOperationResultDto>> SaveSettingsAsync(SystemSettingSaveDto request, CancellationToken cancellationToken = default);
    Task<ApiResult<MasterOperationResultDto>> RestartApiAsync(CancellationToken cancellationToken = default);
    Task<ApiResult<List<SaasPlanDto>>> PlansAsync(CancellationToken cancellationToken = default);
    Task<ApiResult<SaasPlanDto>> SavePlanAsync(SaasPlanDto request, CancellationToken cancellationToken = default);
    Task<ApiResult<List<SystemBannerDto>>> BannersAsync(CancellationToken cancellationToken = default);
    Task<ApiResult<SystemBannerDto>> SaveBannerAsync(SystemBannerDto request, CancellationToken cancellationToken = default);
    Task<ApiResult<MasterOperationResultDto>> DeleteBannerAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResult<MasterOperationResultDto>> NotifyAsync(SegmentNotificationRequestDto request, CancellationToken cancellationToken = default);
    Task<ApiResult<List<ProductChangelogDto>>> ChangelogAsync(CancellationToken cancellationToken = default);
    Task<ApiResult<ProductChangelogDto>> SaveChangelogAsync(ProductChangelogDto request, CancellationToken cancellationToken = default);
    Task<ApiResult<MasterOperationResultDto>> DeleteChangelogAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResult<List<DeveloperAccountDto>>> DeveloperAccountsAsync(CancellationToken cancellationToken = default);
    Task<ApiResult<DeveloperAccountDto>> SaveDeveloperAccountAsync(DeveloperAccountDto request, CancellationToken cancellationToken = default);
    Task<ApiResult<MasterOperationResultDto>> DeleteDeveloperAccountAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResult<DeveloperAccountCheckDto>> CheckDeveloperAccountAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResult<List<DeveloperAccountCheckDto>>> CheckGlobalKeysAsync(CancellationToken cancellationToken = default);
    Task<ApiResult<MenuPermissionMatrixDto>> MenuPermissionsAsync(CancellationToken cancellationToken = default);
    Task<ApiResult<MasterOperationResultDto>> SaveMenuPermissionsAsync(MenuPermissionSaveDto request, CancellationToken cancellationToken = default);
    Task<ApiResult<MasterOperationResultDto>> MoveMenuItemAsync(MenuMoveDto request, CancellationToken cancellationToken = default);
    Task<ApiResult<MasterOperationResultDto>> CreateMenuFolderAsync(MenuFolderCreateDto request, CancellationToken cancellationToken = default);
    Task<ApiResult<MasterOperationResultDto>> DeleteMenuFolderAsync(int folderId, CancellationToken cancellationToken = default);
}

public sealed class MasterApiService : IMasterApiService
{
    private readonly IHttpService _http;
    private readonly ILoadingService _loading;

    public MasterApiService(IHttpService http, ILoadingService loading)
    {
        _http = http;
        _loading = loading;
    }

    public Task<ApiResult<MasterDashboardDto>> DashboardAsync(CancellationToken cancellationToken = default) =>
        _http.GetAsync<MasterDashboardDto>("api/master/dashboard", cancellationToken: cancellationToken);

    public Task<ApiResult<List<TenantSummaryDto>>> TenantsAsync(TenantQueryDto query, CancellationToken cancellationToken = default) =>
        _http.GetAsync<List<TenantSummaryDto>>("api/master/tenants", query, cancellationToken);

    public Task<ApiResult<MasterOperationResultDto>> TenantLifecycleAsync(Guid tenantId, TenantLifecycleRequestDto request, CancellationToken cancellationToken = default) =>
        WithLoading("Aplicando ação...", () => _http.PostAsync<TenantLifecycleRequestDto, MasterOperationResultDto>($"api/master/tenants/{tenantId}/ciclo", request, cancellationToken));

    public Task<ApiResult<MasterOperationResultDto>> TenantPlanAsync(Guid tenantId, TenantPlanChangeDto request, CancellationToken cancellationToken = default) =>
        WithLoading("Alterando plano...", () => _http.PutAsync<TenantPlanChangeDto, MasterOperationResultDto>($"api/master/tenants/{tenantId}/plano", request, cancellationToken));

    public Task<ApiResult<MasterOperationResultDto>> TenantCreditsAsync(Guid tenantId, TenantCreditAdjustDto request, CancellationToken cancellationToken = default) =>
        WithLoading("Ajustando créditos...", () => _http.PostAsync<TenantCreditAdjustDto, MasterOperationResultDto>($"api/master/tenants/{tenantId}/creditos", request, cancellationToken));

    public Task<ApiResult<TenantStorageDto>> TenantStorageAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        _http.GetAsync<TenantStorageDto>($"api/master/tenants/{tenantId}/armazenamento", cancellationToken: cancellationToken);

    public Task<ApiResult<MasterOperationResultDto>> TenantPurgeAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        WithLoading("Purgando mídias...", () => _http.PostAsync<object, MasterOperationResultDto>($"api/master/tenants/{tenantId}/armazenamento/purgar", new { }, cancellationToken));

    public Task<ApiResult<MasterOperationResultDto>> TenantSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        WithLoading("Gerando assinatura no gateway...", () => _http.PostAsync<object, MasterOperationResultDto>($"api/master/tenants/{tenantId}/assinatura", new { }, cancellationToken));

    public Task<ApiResult<List<MasterOperatorDto>>> OperatorsAsync(CancellationToken cancellationToken = default) =>
        _http.GetAsync<List<MasterOperatorDto>>("api/master/operadores", cancellationToken: cancellationToken);

    public Task<ApiResult<MasterOperatorDto>> SaveOperatorAsync(int? id, MasterOperatorSaveDto request, CancellationToken cancellationToken = default) =>
        WithLoading("Salvando operador...", () => id is > 0
            ? _http.PutAsync<MasterOperatorSaveDto, MasterOperatorDto>($"api/master/operadores/{id}", request, cancellationToken)
            : _http.PostAsync<MasterOperatorSaveDto, MasterOperatorDto>("api/master/operadores", request, cancellationToken));

    public Task<ApiResult<MasterOperationResultDto>> SetOperatorActiveAsync(int id, bool active, CancellationToken cancellationToken = default) =>
        _http.PostAsync<object, MasterOperationResultDto>($"api/master/operadores/{id}/ativo?value={active.ToString().ToLowerInvariant()}", new { }, cancellationToken);

    public Task<ApiResult<MasterPermissionMatrixDto>> PermissionsAsync(CancellationToken cancellationToken = default) =>
        _http.GetAsync<MasterPermissionMatrixDto>("api/master/permissoes", cancellationToken: cancellationToken);

    public Task<ApiResult<List<MasterAuditLogDto>>> AuditAsync(MasterAuditQueryDto query, CancellationToken cancellationToken = default) =>
        _http.GetAsync<List<MasterAuditLogDto>>("api/master/auditoria", query, cancellationToken);

    public Task<ApiResult<List<SystemSettingItemDto>>> SettingsAsync(CancellationToken cancellationToken = default) =>
        _http.GetAsync<List<SystemSettingItemDto>>("api/master/configuracoes", cancellationToken: cancellationToken);

    public Task<ApiResult<MasterOperationResultDto>> SaveSettingsAsync(SystemSettingSaveDto request, CancellationToken cancellationToken = default) =>
        WithLoading("Criptografando e salvando chaves...", () => _http.PutAsync<SystemSettingSaveDto, MasterOperationResultDto>("api/master/configuracoes", request, cancellationToken));

    public Task<ApiResult<MasterOperationResultDto>> RestartApiAsync(CancellationToken cancellationToken = default) =>
        _http.PostAsync<object, MasterOperationResultDto>("api/master/configuracoes/reiniciar-api", new { }, cancellationToken);

    public Task<ApiResult<List<SaasPlanDto>>> PlansAsync(CancellationToken cancellationToken = default) =>
        _http.GetAsync<List<SaasPlanDto>>("api/master/planos", cancellationToken: cancellationToken);

    public Task<ApiResult<SaasPlanDto>> SavePlanAsync(SaasPlanDto request, CancellationToken cancellationToken = default) =>
        WithLoading("Salvando plano...", () => _http.PutAsync<SaasPlanDto, SaasPlanDto>("api/master/planos", request, cancellationToken));

    public Task<ApiResult<List<SystemBannerDto>>> BannersAsync(CancellationToken cancellationToken = default) =>
        _http.GetAsync<List<SystemBannerDto>>("api/master/avisos", cancellationToken: cancellationToken);

    public Task<ApiResult<SystemBannerDto>> SaveBannerAsync(SystemBannerDto request, CancellationToken cancellationToken = default) =>
        _http.PutAsync<SystemBannerDto, SystemBannerDto>("api/master/avisos", request, cancellationToken);

    public Task<ApiResult<MasterOperationResultDto>> DeleteBannerAsync(int id, CancellationToken cancellationToken = default) =>
        _http.DeleteAsync<MasterOperationResultDto>($"api/master/avisos/{id}", cancellationToken: cancellationToken);

    public Task<ApiResult<MasterOperationResultDto>> NotifyAsync(SegmentNotificationRequestDto request, CancellationToken cancellationToken = default) =>
        WithLoading("Enviando notificação...", () => _http.PostAsync<SegmentNotificationRequestDto, MasterOperationResultDto>("api/master/notificacoes", request, cancellationToken));

    public Task<ApiResult<List<ProductChangelogDto>>> ChangelogAsync(CancellationToken cancellationToken = default) =>
        _http.GetAsync<List<ProductChangelogDto>>("api/master/changelog", cancellationToken: cancellationToken);

    public Task<ApiResult<ProductChangelogDto>> SaveChangelogAsync(ProductChangelogDto request, CancellationToken cancellationToken = default) =>
        _http.PutAsync<ProductChangelogDto, ProductChangelogDto>("api/master/changelog", request, cancellationToken);

    public Task<ApiResult<MasterOperationResultDto>> DeleteChangelogAsync(int id, CancellationToken cancellationToken = default) =>
        _http.DeleteAsync<MasterOperationResultDto>($"api/master/changelog/{id}", cancellationToken: cancellationToken);

    public Task<ApiResult<List<DeveloperAccountDto>>> DeveloperAccountsAsync(CancellationToken cancellationToken = default) =>
        _http.GetAsync<List<DeveloperAccountDto>>("api/master/marketplace-accounts", cancellationToken: cancellationToken);

    public Task<ApiResult<DeveloperAccountDto>> SaveDeveloperAccountAsync(DeveloperAccountDto request, CancellationToken cancellationToken = default) =>
        WithLoading("Criptografando e salvando conta...", () => _http.PutAsync<DeveloperAccountDto, DeveloperAccountDto>("api/master/marketplace-accounts", request, cancellationToken));

    public Task<ApiResult<MasterOperationResultDto>> DeleteDeveloperAccountAsync(int id, CancellationToken cancellationToken = default) =>
        _http.DeleteAsync<MasterOperationResultDto>($"api/master/marketplace-accounts/{id}", cancellationToken: cancellationToken);

    public Task<ApiResult<DeveloperAccountCheckDto>> CheckDeveloperAccountAsync(int id, CancellationToken cancellationToken = default) =>
        WithLoading("Testando conexão com o marketplace...", () => _http.PostAsync<object, DeveloperAccountCheckDto>($"api/master/marketplace-accounts/{id}/testar", new { }, cancellationToken));

    public Task<ApiResult<List<DeveloperAccountCheckDto>>> CheckGlobalKeysAsync(CancellationToken cancellationToken = default) =>
        WithLoading("Testando chaves do cofre...", () => _http.PostAsync<object, List<DeveloperAccountCheckDto>>("api/master/configuracoes/saude-chaves", new { }, cancellationToken));

    public Task<ApiResult<MenuPermissionMatrixDto>> MenuPermissionsAsync(CancellationToken cancellationToken = default) =>
        _http.GetAsync<MenuPermissionMatrixDto>("api/master/menu-permissions", cancellationToken: cancellationToken);

    public Task<ApiResult<MasterOperationResultDto>> SaveMenuPermissionsAsync(MenuPermissionSaveDto request, CancellationToken cancellationToken = default) =>
        WithLoading("Salvando matriz de menus...", () => _http.PutAsync<MenuPermissionSaveDto, MasterOperationResultDto>("api/master/menu-permissions", request, cancellationToken));

    public Task<ApiResult<MasterOperationResultDto>> MoveMenuItemAsync(MenuMoveDto request, CancellationToken cancellationToken = default) =>
        _http.PostAsync<MenuMoveDto, MasterOperationResultDto>("api/master/menu-permissions/mover", request, cancellationToken);

    public Task<ApiResult<MasterOperationResultDto>> CreateMenuFolderAsync(MenuFolderCreateDto request, CancellationToken cancellationToken = default) =>
        WithLoading("Criando pasta...", () => _http.PostAsync<MenuFolderCreateDto, MasterOperationResultDto>("api/master/menu-permissions/pastas", request, cancellationToken));

    public Task<ApiResult<MasterOperationResultDto>> DeleteMenuFolderAsync(int folderId, CancellationToken cancellationToken = default) =>
        _http.DeleteAsync<MasterOperationResultDto>($"api/master/menu-permissions/pastas/{folderId}", cancellationToken: cancellationToken);

    private async Task<T> WithLoading<T>(string message, Func<Task<T>> action)
    {
        using var _ = _loading.BeginScope(message);
        return await action();
    }
}
