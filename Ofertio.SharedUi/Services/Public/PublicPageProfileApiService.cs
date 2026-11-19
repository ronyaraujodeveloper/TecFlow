using Ofertio.Business.Dto;
using Ofertio.SharedUi.Models;
using Ofertio.SharedUi.Services.Http;

namespace Ofertio.SharedUi.Services.Public;

public interface IPublicPageProfileApiService
{
    Task<ApiResult<PublicPageProfileResponseDto>> GetAsync(CancellationToken cancellationToken = default);

    Task<ApiResult<PublicPageProfileResponseDto>> UploadAvatarAsync(
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default);

    Task<ApiResult<PublicPageProfileResponseDto>> RemoveAvatarAsync(CancellationToken cancellationToken = default);

    Task<ApiResult<PublicPageProfileResponseDto>> CreateLinkAsync(JoinLinkCreateDto request, CancellationToken cancellationToken = default);

    Task<ApiResult<PublicPageProfileResponseDto>> UpdateLinkAsync(int id, JoinLinkUpdateDto request, CancellationToken cancellationToken = default);

    Task<ApiResult<PublicPageProfileResponseDto>> DeleteLinkAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class PublicPageProfileApiService : IPublicPageProfileApiService
{
    private const string BasePath = "api/afiliados/pagina-publica";

    private readonly IHttpService _http;

    public PublicPageProfileApiService(IHttpService http)
    {
        _http = http;
    }

    public Task<ApiResult<PublicPageProfileResponseDto>> GetAsync(CancellationToken cancellationToken = default) =>
        _http.GetAsync<PublicPageProfileResponseDto>(BasePath, cancellationToken: cancellationToken);

    public Task<ApiResult<PublicPageProfileResponseDto>> UploadAvatarAsync(
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default) =>
        _http.PostFileAsync<PublicPageProfileResponseDto>($"{BasePath}/foto", "file", fileName, contentType, content, cancellationToken);

    public Task<ApiResult<PublicPageProfileResponseDto>> RemoveAvatarAsync(CancellationToken cancellationToken = default) =>
        _http.DeleteAsync<PublicPageProfileResponseDto>($"{BasePath}/foto", cancellationToken: cancellationToken);

    public Task<ApiResult<PublicPageProfileResponseDto>> CreateLinkAsync(JoinLinkCreateDto request, CancellationToken cancellationToken = default) =>
        _http.PostAsync<JoinLinkCreateDto, PublicPageProfileResponseDto>($"{BasePath}/links", request, cancellationToken);

    public Task<ApiResult<PublicPageProfileResponseDto>> UpdateLinkAsync(int id, JoinLinkUpdateDto request, CancellationToken cancellationToken = default) =>
        _http.PutAsync<JoinLinkUpdateDto, PublicPageProfileResponseDto>($"{BasePath}/links/{id}", request, cancellationToken);

    public Task<ApiResult<PublicPageProfileResponseDto>> DeleteLinkAsync(int id, CancellationToken cancellationToken = default) =>
        _http.DeleteAsync<PublicPageProfileResponseDto>($"{BasePath}/links/{id}", cancellationToken: cancellationToken);
}
