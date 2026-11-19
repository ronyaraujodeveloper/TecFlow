using Ofertio.SharedUi.Models;

namespace Ofertio.SharedUi.Services.Http;

public interface IHttpService
{
    Task<ApiResult<TResponse>> GetAsync<TResponse>(
        string relativeUrl,
        object? queryFilter = null,
        CancellationToken cancellationToken = default);

    Task<ApiResult<TResponse>> PostAsync<TRequest, TResponse>(
        string relativeUrl,
        TRequest body,
        CancellationToken cancellationToken = default);

    Task<ApiResult<TResponse>> PutAsync<TRequest, TResponse>(
        string relativeUrl,
        TRequest body,
        CancellationToken cancellationToken = default);

    /// <summary>Envia um arquivo (multipart/form-data) com o token do usuário.</summary>
    Task<ApiResult<TResponse>> PostFileAsync<TResponse>(
        string relativeUrl,
        string fieldName,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default);

    Task<ApiResult<TResponse>> DeleteAsync<TResponse>(
        string relativeUrl,
        object? queryFilter = null,
        CancellationToken cancellationToken = default);
}
