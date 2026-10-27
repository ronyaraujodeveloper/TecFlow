using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Ofertio.Business.Dto.Auth;
using Ofertio.SharedUi.Serialization;
using Ofertio.SharedUi.Services.Http;
using Ofertio.SharedUi.Services.UI;

namespace Ofertio.SharedUi.Services.Auth;

public interface IAccountSecurityApiService
{
    Task<AuthProviderResponseDto> GetProviderStatusAsync(CancellationToken cancellationToken = default);

    Task<AuthProviderResponseDto> UnlinkProviderAsync(
        string provider,
        CancellationToken cancellationToken = default);

    Task<AuthProviderResponseDto> ChangePasswordAsync(
        ChangePasswordDto request,
        CancellationToken cancellationToken = default);

    Task<AuthProviderResponseDto> ChangeStateAsync(
        ChangeStateDto request,
        CancellationToken cancellationToken = default);

    /// <summary>Status sem overlay de carregamento (usado pelo relógio do usuário em segundo plano).</summary>
    Task<AuthProviderResponseDto> GetStatusQuietlyAsync(CancellationToken cancellationToken = default);
}

public class AccountSecurityApiService : IAccountSecurityApiService
{
    private static readonly JsonSerializerOptions JsonOptions = OfertioJsonOptions.Http;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAccessTokenProvider _accessTokenProvider;
    private readonly ILoadingService _loadingService;
    private readonly ILogger<AccountSecurityApiService> _logger;

    public AccountSecurityApiService(
        IHttpClientFactory httpClientFactory,
        IAccessTokenProvider accessTokenProvider,
        ILoadingService loadingService,
        ILogger<AccountSecurityApiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _accessTokenProvider = accessTokenProvider;
        _loadingService = loadingService;
        _logger = logger;
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

    public Task<AuthProviderResponseDto> ChangeStateAsync(
        ChangeStateDto request,
        CancellationToken cancellationToken = default)
    {
        using var _ = _loadingService.BeginScope("Salvando fuso horário...");
        return SendEnvelopeAsync(HttpMethod.Put, "api/auth/state", request, cancellationToken);
    }

    public Task<AuthProviderResponseDto> GetStatusQuietlyAsync(CancellationToken cancellationToken = default) =>
        SendEnvelopeAsync(HttpMethod.Get, "api/auth/status", body: null, cancellationToken);

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

            var accessToken = await _accessTokenProvider.GetAccessTokenAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken.Trim());
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
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout em {Method} {RelativeUrl}.", method, relativeUrl);
            return new AuthProviderResponseDto
            {
                Status = false,
                Descricao = "Tempo limite excedido ao contactar o servidor."
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Falha HTTP em {Method} {RelativeUrl}.", method, relativeUrl);
            return new AuthProviderResponseDto
            {
                Status = false,
                Descricao = "Não foi possível contactar o servidor. Verifique se a API está em execução."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado em {Method} {RelativeUrl}.", method, relativeUrl);
            return new AuthProviderResponseDto
            {
                Status = false,
                Descricao = "Ocorreu um erro inesperado ao comunicar com a API."
            };
        }
    }

    private T? TryDeserialize<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Falha ao desserializar JSON. Payload={Payload}", json);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao desserializar JSON. Payload={Payload}", json);
            return default;
        }
    }
}
