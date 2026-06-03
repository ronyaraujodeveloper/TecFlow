using TecFlow.Portal.Models;

namespace TecFlow.Portal.Services.Http;

public interface IHttpService
{
    Task<ApiResult<TResponse>> GetAsync<TResponse>(string relativeUrl, CancellationToken cancellationToken = default);
    Task<ApiResult<TResponse>> PostAsync<TRequest, TResponse>(
        string relativeUrl,
        TRequest body,
        CancellationToken cancellationToken = default);
}
