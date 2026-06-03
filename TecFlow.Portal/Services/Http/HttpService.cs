using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Security.Claims;
using TecFlow.Portal.Models;
using TecFlow.Portal.Models.Responses;
using TecFlow.Portal.Security;
using TecFlow.Portal.Services.State;

namespace TecFlow.Portal.Services.Http;

public class HttpService : IHttpService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISessionStateService _sessionState;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpService(
        IHttpClientFactory httpClientFactory,
        ISessionStateService sessionState,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClientFactory = httpClientFactory;
        _sessionState = sessionState;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<ApiResult<TResponse>> GetAsync<TResponse>(string relativeUrl, CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(HttpMethod.Get, relativeUrl, body: null, cancellationToken);

    public Task<ApiResult<TResponse>> PostAsync<TRequest, TResponse>(
        string relativeUrl,
        TRequest body,
        CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(HttpMethod.Post, relativeUrl, body, cancellationToken);

    private async Task<ApiResult<TResponse>> SendAsync<TResponse>(
        HttpMethod method,
        string relativeUrl,
        object? body,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Orquestrador");
            using var request = new HttpRequestMessage(method, relativeUrl);

            var accessToken = ResolveAccessToken();
            if (!string.IsNullOrEmpty(accessToken))
            {
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            }

            if (body is not null)
            {
                request.Content = JsonContent.Create(body);
            }

            using var response = await client.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                if (string.IsNullOrWhiteSpace(content))
                {
                    return ApiResult<TResponse>.Fail("Resposta vazia do servidor.", (int)response.StatusCode);
                }

                var data = JsonSerializer.Deserialize<TResponse>(content, JsonOptions);
                if (data is null)
                {
                    return ApiResult<TResponse>.Fail("Não foi possível interpretar a resposta do servidor.", (int)response.StatusCode);
                }

                return ApiResult<TResponse>.Ok(data);
            }

            var apiError = TryDeserialize<ApiErrorResponse>(content);
            var message = apiError?.Message ?? $"Erro na API ({(int)response.StatusCode}).";
            return ApiResult<TResponse>.Fail(message, (int)response.StatusCode, apiError?.ErrorCode);
        }
        catch (TaskCanceledException)
        {
            return ApiResult<TResponse>.Fail("Tempo limite excedido ao contactar o servidor.", isOffline: true);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is null or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout)
        {
            return ApiResult<TResponse>.Fail("Servidor indisponível. Tente novamente em instantes.", isOffline: true);
        }
        catch (HttpRequestException)
        {
            return ApiResult<TResponse>.Fail("Não foi possível contactar o servidor. Verifique se o Orquestrador está em execução.", isOffline: true);
        }
        catch (Exception)
        {
            return ApiResult<TResponse>.Fail("Ocorreu um erro inesperado ao comunicar com a API.");
        }
    }

    private string? ResolveAccessToken()
    {
        if (!string.IsNullOrEmpty(_sessionState.AccessToken))
        {
            return _sessionState.AccessToken;
        }

        return _httpContextAccessor.HttpContext?.User.FindFirstValue(TecFlowClaimTypes.AccessToken);
    }

    private static T? TryDeserialize<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch
        {
            return default;
        }
    }
}
