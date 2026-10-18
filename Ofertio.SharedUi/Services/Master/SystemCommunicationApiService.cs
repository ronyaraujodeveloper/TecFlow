using Ofertio.Business.Dto;
using Ofertio.SharedUi.Models;
using Ofertio.SharedUi.Services.Http;

namespace Ofertio.SharedUi.Services.Master;

public interface ISystemCommunicationApiService
{
    Task<ApiResult<List<SystemBannerDto>>> ActiveBannersAsync(CancellationToken cancellationToken = default);
    Task<ApiResult<List<TenantNotificationDto>>> NotificationsAsync(CancellationToken cancellationToken = default);
    Task<ApiResult<MasterOperationResultDto>> MarkReadAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResult<List<ProductChangelogDto>>> ChangelogAsync(CancellationToken cancellationToken = default);
    Task<ApiResult<List<ClientMenuNodeDto>>> ClientMenuAsync(bool clientView = false, CancellationToken cancellationToken = default);
}

public sealed class SystemCommunicationApiService : ISystemCommunicationApiService
{
    private readonly IHttpService _http;

    public SystemCommunicationApiService(IHttpService http)
    {
        _http = http;
    }

    public Task<ApiResult<List<SystemBannerDto>>> ActiveBannersAsync(CancellationToken cancellationToken = default) =>
        _http.GetAsync<List<SystemBannerDto>>("api/sistema/avisos", cancellationToken: cancellationToken);

    public Task<ApiResult<List<TenantNotificationDto>>> NotificationsAsync(CancellationToken cancellationToken = default) =>
        _http.GetAsync<List<TenantNotificationDto>>("api/sistema/notificacoes", cancellationToken: cancellationToken);

    public Task<ApiResult<MasterOperationResultDto>> MarkReadAsync(int id, CancellationToken cancellationToken = default) =>
        _http.PostAsync<object, MasterOperationResultDto>($"api/sistema/notificacoes/{id}/lida", new { }, cancellationToken);

    public Task<ApiResult<List<ProductChangelogDto>>> ChangelogAsync(CancellationToken cancellationToken = default) =>
        _http.GetAsync<List<ProductChangelogDto>>("api/sistema/novidades", cancellationToken: cancellationToken);

    public Task<ApiResult<List<ClientMenuNodeDto>>> ClientMenuAsync(bool clientView = false, CancellationToken cancellationToken = default) =>
        _http.GetAsync<List<ClientMenuNodeDto>>(clientView ? "api/sistema/menu?visao=cliente" : "api/sistema/menu", cancellationToken: cancellationToken);
}
