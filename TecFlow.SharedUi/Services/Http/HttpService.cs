using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Dto;
using TecFlow.SharedUi.Extensions;
using TecFlow.SharedUi.Models;
using TecFlow.SharedUi.Models.Responses;
using TecFlow.SharedUi.Serialization;

namespace TecFlow.SharedUi.Services.Http;

public class HttpService : IHttpService
{
    private static readonly JsonSerializerOptions JsonOptions = TecFlowJsonOptions.Http;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAccessTokenProvider _accessTokenProvider;
    private readonly ILogger<HttpService> _logger;

    public HttpService(
        IHttpClientFactory httpClientFactory,
        IAccessTokenProvider accessTokenProvider,
        ILogger<HttpService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _accessTokenProvider = accessTokenProvider;
        _logger = logger;
    }

    public Task<ApiResult<TResponse>> GetAsync<TResponse>(
        string relativeUrl,
        object? queryFilter = null,
        CancellationToken cancellationToken = default)
    {
        var url = relativeUrl.AppendQueryString(queryFilter);
        return SendAsync<TResponse>(HttpMethod.Get, url, body: null, cancellationToken);
    }

    public Task<ApiResult<TResponse>> PostAsync<TRequest, TResponse>(
        string relativeUrl,
        TRequest body,
        CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(HttpMethod.Post, relativeUrl, body, cancellationToken);

    public Task<ApiResult<TResponse>> PutAsync<TRequest, TResponse>(
        string relativeUrl,
        TRequest body,
        CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(HttpMethod.Put, relativeUrl, body, cancellationToken);

    public Task<ApiResult<TResponse>> DeleteAsync<TResponse>(
        string relativeUrl,
        object? queryFilter = null,
        CancellationToken cancellationToken = default)
    {
        var url = relativeUrl.AppendQueryString(queryFilter);
        return SendAsync<TResponse>(HttpMethod.Delete, url, body: null, cancellationToken);
    }

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

            if (string.IsNullOrWhiteSpace(content))
            {
                return ApiResult<TResponse>.Fail(
                    FormatIisConnectionError(response.StatusCode),
                    (int)response.StatusCode);
            }

            if (response.IsSuccessStatusCode)
            {

                TResponse? data;
                try
                {
                    data = JsonSerializer.Deserialize<TResponse>(content, JsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Falha ao desserializar JSON. Payload={Payload}", Truncate(content));
                    return ApiResult<TResponse>.Fail(FormatIisError(content), (int)response.StatusCode);
                }

                if (data is null)
                {
                    _logger.LogError("Resposta JSON nula após desserialização. Payload={Payload}", Truncate(content));
                    return ApiResult<TResponse>.Fail(FormatIisError(content), (int)response.StatusCode);
                }

                return ApiResult<TResponse>.Ok(data);
            }

            if (!LooksLikeJson(content))
            {
                return ApiResult<TResponse>.Fail(FormatIisError(content), (int)response.StatusCode);
            }

            var apiError = TryDeserialize<ApiErrorResponse>(content);
            var affiliateError = TryDeserialize<GerarLinkAfiliadoResponseDto>(content);
            var marketplaceError = TryDeserialize<MarketplaceAccountResponseDto>(content);
            var lojaError = TryDeserialize<IntegracaoLojaResponseDto>(content);
            var responseDto = TryDeserialize<ResponseDto>(content);
            var message = FirstNonEmpty(
                marketplaceError?.Descricao,
                lojaError?.Descricao,
                responseDto?.Descricao,
                apiError?.Message,
                affiliateError?.Message,
                ReadProblemDetail(content),
                FormatIisError(content),
                $"Erro na API ({(int)response.StatusCode}).");
            return ApiResult<TResponse>.Fail(message, (int)response.StatusCode, apiError?.ErrorCode);
        }
        catch (TaskCanceledException ex)
        {
            LogHttpFailure(ex, method, relativeUrl, "timeout");
            return ApiResult<TResponse>.Fail("Tempo limite excedido ao contactar o servidor.", isOffline: true);
        }
        catch (HttpRequestException ex)
        {
            LogHttpFailure(ex, method, relativeUrl, "http");
            return ApiResult<TResponse>.Fail(FormatIisConnectionError(ex.StatusCode), (int?)ex.StatusCode, isOffline: true);
        }
        catch (Exception ex)
        {
            LogHttpFailure(ex, method, relativeUrl, "inesperado");
            return ApiResult<TResponse>.Fail("Ocorreu um erro inesperado ao comunicar com a API.");
        }
    }

    private void LogHttpFailure(Exception ex, HttpMethod method, string relativeUrl, string category)
    {
        var client = _httpClientFactory.CreateClient("Orquestrador");
        _logger.LogError(
            ex,
            "Falha HTTP ({Category}) {Method} {BaseAddress}{RelativeUrl}",
            category,
            method,
            client.BaseAddress,
            relativeUrl);
        Console.WriteLine(ex.ToString());
    }

    private string? ResolveAccessToken() => _accessTokenProvider.GetAccessToken();

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

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return "Erro na API.";
    }

    private static string? ReadProblemDetail(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var root = document.RootElement;
            return FirstNonEmpty(
                ReadJsonString(root, "descricao"),
                ReadJsonString(root, "detail"),
                ReadJsonString(root, "title"),
                ReadJsonString(root, "error"),
                ReadJsonString(root, "message"));
        }
        catch
        {
            return null;
        }
    }

    private static string? ReadJsonString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() : property.ToString();
    }

    private static bool LooksLikeJson(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        var trimmed = content.TrimStart();
        return trimmed.StartsWith('{') || trimmed.StartsWith('[');
    }

    internal static string FormatIisConnectionError(HttpStatusCode? statusCode) =>
        $"Erro de Conexão no IIS (HTTP {statusCode}): Verifique se a API na porta 5001 está online e com CORS liberado.";

    internal static string FormatIisError(string? content)
    {
        var snippet = Truncate(content, 200);
        return string.IsNullOrWhiteSpace(snippet)
            ? FormatIisConnectionError(null)
            : "Erro retornado pelo IIS: " + snippet;
    }

    private static string Truncate(string? value, int maxLength = 2000)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value ?? string.Empty;
        }

        return value[..maxLength];
    }
}
