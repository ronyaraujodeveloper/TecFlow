using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TecFlow.WebUi.Models;
using TecFlow.WebUi.Models.Enums;
using TecFlow.WebUi.Models.Requests;
using TecFlow.WebUi.Models.Responses;

namespace TecFlow.WebUi.Services.Auth;

public class OrquestradorAuthBridge : IOrquestradorAuthBridge
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;

    public OrquestradorAuthBridge(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ApiResult<AuthTokenResponse>> LoginAsync(
        LoginPlatform platform,
        PlatformAuthRequest request,
        CancellationToken cancellationToken = default)
    {
        var endpoint = platform switch
        {
            LoginPlatform.TikTok => "api/auth/tiktok/login",
            LoginPlatform.Shopee => "api/auth/shopee/login",
            _ => throw new ArgumentOutOfRangeException(nameof(platform))
        };

        try
        {
            var client = _httpClientFactory.CreateClient("Orquestrador");
            using var response = await client.PostAsJsonAsync(endpoint, request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = JsonSerializer.Deserialize<AuthTokenResponse>(content, JsonOptions);
                return data is null
                    ? ApiResult<AuthTokenResponse>.Fail("Resposta inválida do Orquestrador.")
                    : ApiResult<AuthTokenResponse>.Ok(data);
            }

            var apiError = TryDeserialize<ApiErrorResponse>(content);
            return ApiResult<AuthTokenResponse>.Fail(
                apiError?.Message ?? $"Erro na API ({(int)response.StatusCode}).",
                (int)response.StatusCode,
                apiError?.ErrorCode);
        }
        catch (TaskCanceledException)
        {
            return ApiResult<AuthTokenResponse>.Fail("Tempo limite excedido.", isOffline: true);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is null or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout)
        {
            return ApiResult<AuthTokenResponse>.Fail("Servidor indisponível.", isOffline: true);
        }
        catch (HttpRequestException)
        {
            return ApiResult<AuthTokenResponse>.Fail(
                "Não foi possível contactar o Orquestrador.",
                isOffline: true);
        }
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
