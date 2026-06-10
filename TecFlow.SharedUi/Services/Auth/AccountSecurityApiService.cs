using System.Net.Http.Json;
using System.Text.Json;
using TecFlow.Business.Dto.Auth;
using TecFlow.SharedUi.Services.Http;
using TecFlow.SharedUi.Services.UI;

namespace TecFlow.SharedUi.Services.Auth;

public interface IAccountSecurityApiService
{
    Task<AuthProviderResponseDto> GetProviderStatusAsync(CancellationToken cancellationToken = default);

    Task<AuthProviderResponseDto> UnlinkProviderAsync(
        string provider,
        CancellationToken cancellationToken = default);

    Task<AuthProviderResponseDto> ChangePasswordAsync(
        ChangePasswordDto request,
        CancellationToken cancellationToken = default);
}

public class AccountSecurityApiService : IAccountSecurityApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAccessTokenProvider _accessTokenProvider;
    private readonly ILoadingService _loadingService;

    public AccountSecurityApiService(
        IHttpClientFactory httpClientFactory,
        IAccessTokenProvider accessTokenProvider,
        ILoadingService loadingService)
    {
        _httpClientFactory = httpClientFactory;
        _accessTokenProvider = accessTokenProvider;
        _loadingService = loadingService;
    }

    public Task<AuthProviderResponseDto> GetProviderStatusAsync(CancellationToken cancellationToken = default)
    {
        using var _ = _loadingService.BeginScope("Carregando métodos de acesso...");
        return SendEnvelopeAsync(HttpMethod.Get, "api/auth/status", body: null, cancellationToken);
    }

    public Task<AuthProviderResponseDto> UnlinkProviderAsync(
        string provider,
        CancellationToken cancellationToken = default)
    {
        using var _ = _loadingService.BeginScope("Desvinculando provedor...");
        var url = $"api/auth/providers/desvincular?provider={Uri.EscapeDataString(provider)}";
        return SendEnvelopeAsync(HttpMethod.Delete, url, body: null, cancellationToken);
    }

    public Task<AuthProviderResponseDto> ChangePasswordAsync(
        ChangePasswordDto request,
        CancellationToken cancellationToken = default)
    {
        using var _ = _loadingService.BeginScope("Atualizando senha...");
        return SendEnvelopeAsync(HttpMethod.Put, "api/auth/change-password", request, cancellationToken);
    }

    private async Task<AuthProviderResponseDto> SendEnvelopeAsync(
        HttpMethod method,
        string relativeUrl,
        object? body,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Orquestrador");
            using var request = new HttpRequestMessage(method, relativeUrl);

            var accessToken = _accessTokenProvider.GetAccessToken();
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
            var envelope = TryDeserialize<AuthProviderResponseDto>(content);

            if (envelope is not null)
            {
                return envelope;
            }

            return new AuthProviderResponseDto
            {
                Status = false,
                Descricao = "Não foi possível interpretar a resposta do servidor."
            };
        }
        catch (TaskCanceledException)
        {
            return new AuthProviderResponseDto
            {
                Status = false,
                Descricao = "Tempo limite excedido ao contactar o servidor."
            };
        }
        catch (HttpRequestException)
        {
            return new AuthProviderResponseDto
            {
                Status = false,
                Descricao = "Não foi possível contactar o servidor. Verifique se a API está em execução."
            };
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
