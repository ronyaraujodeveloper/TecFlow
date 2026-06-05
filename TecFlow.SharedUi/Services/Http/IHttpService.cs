using TecFlow.SharedUi.Models;

namespace TecFlow.SharedUi.Services.Http;

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
}
