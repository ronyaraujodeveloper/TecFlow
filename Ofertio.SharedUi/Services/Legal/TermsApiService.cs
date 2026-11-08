using Ofertio.Business.Dto;
using Ofertio.Database.Filter;
using Ofertio.SharedUi.Models;
using Ofertio.SharedUi.Services.Http;

namespace Ofertio.SharedUi.Services.Legal;

public interface ITermsApiService
{
    Task<ApiResult<TermsVersionResponseDto>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<ApiResult<UserTermsAcceptanceResponseDto>> GetStatusAsync(CancellationToken cancellationToken = default);

    Task<ApiResult<UserTermsAcceptanceResponseDto>> AcceptAsync(
        UserTermsAcceptanceDto request,
        CancellationToken cancellationToken = default);
}

public sealed class TermsApiService : ITermsApiService
{
    private readonly IHttpService _http;

    public TermsApiService(IHttpService http)
    {
        _http = http;
    }

    public Task<ApiResult<TermsVersionResponseDto>> GetActiveAsync(CancellationToken cancellationToken = default) =>
        _http.GetAsync<TermsVersionResponseDto>("api/legal/active", cancellationToken: cancellationToken);

    public Task<ApiResult<UserTermsAcceptanceResponseDto>> GetStatusAsync(CancellationToken cancellationToken = default) =>
        _http.GetAsync<UserTermsAcceptanceResponseDto>("api/legal/acceptance", cancellationToken: cancellationToken);

    public Task<ApiResult<UserTermsAcceptanceResponseDto>> AcceptAsync(
        UserTermsAcceptanceDto request,
        CancellationToken cancellationToken = default) =>
        _http.PostAsync<UserTermsAcceptanceDto, UserTermsAcceptanceResponseDto>("api/legal/acceptance", request, cancellationToken);
}
