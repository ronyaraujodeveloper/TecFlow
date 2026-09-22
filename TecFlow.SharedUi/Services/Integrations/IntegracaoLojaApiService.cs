using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Dto;
using TecFlow.Business.Integrations.Auth;
using TecFlow.Core.Enums;
using TecFlow.Database.Filter;
using TecFlow.SharedUi.Extensions;
using TecFlow.SharedUi.Serialization;
using TecFlow.SharedUi.Services.Http;
using TecFlow.SharedUi.Services.UI;

namespace TecFlow.SharedUi.Services.Integrations;

public interface IIntegracaoLojaApiService
{
    Task<IntegracaoLojaResponseDto> ListAsync(
        IntegracaoLojaFilter? filter = null,
        CancellationToken cancellationToken = default);

    Task<IntegracaoLojaResponseDto> LinkAsync(
        IntegracaoLojaDto request,
        CancellationToken cancellationToken = default);

    Task<IntegracaoLojaResponseDto> UnlinkAsync(
        int integrationId,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? AuthorizationUrl, string? ErrorMessage)> GetAuthorizationUrlAsync(
        MarketplaceType platformType,
        string redirectUri,
        string? state = null,
        string? friendlyName = null,
        string? lojaId = null,
        CancellationToken cancellationToken = default);
}

public class IntegracaoLojaApiService : IIntegracaoLojaApiService
{
    private static readonly JsonSerializerOptions JsonOptions = TecFlowJsonOptions.Http;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAccessTokenProvider _accessTokenProvider;
    private readonly ILoadingService _loadingService;
    private readonly ILogger<IntegracaoLojaApiService> _logger;

    public IntegracaoLojaApiService(
        IHttpClientFactory httpClientFactory,
        IAccessTokenProvider accessTokenProvider,
        ILoadingService loadingService,
        ILogger<IntegracaoLojaApiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _accessTokenProvider = accessTokenProvider;
        _loadingService = loadingService;
        _logger = logger;
    }

    public Task<IntegracaoLojaResponseDto> ListAsync(
        IntegracaoLojaFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        using var _ = _loadingService.BeginScope("Carregando lojas integradas...");
        filter ??= new IntegracaoLojaFilter { Page = 1, PageSize = 100 };
        var url = "api/marketplace-auth/lojas".AppendQueryString(filter);
        return SendEnvelopeAsync(HttpMethod.Get, url, null, cancellationToken);
    }

    public Task<IntegracaoLojaResponseDto> LinkAsync(
        IntegracaoLojaDto request,
        CancellationToken cancellationToken = default)
    {
        using var _ = _loadingService.BeginScope("Vinculando nova loja...");
        return SendEnvelopeAsync(HttpMethod.Post, "api/marketplace-auth/vincular-manual", request, cancellationToken);
    }

    public Task<IntegracaoLojaResponseDto> UnlinkAsync(
        int integrationId,
        CancellationToken cancellationToken = default)
    {
        using var _ = _loadingService.BeginScope("Desconectando loja...");
        return SendEnvelopeAsync(HttpMethod.Delete, $"api/integracoes/lojas/{integrationId}", null, cancellationToken);
    }

    public async Task<(bool Success, string? AuthorizationUrl, string? ErrorMessage)> GetAuthorizationUrlAsync(
        MarketplaceType platformType,
        string redirectUri,
        string? state = null,
        string? friendlyName = null,
        string? lojaId = null,
        CancellationToken cancellationToken = default)
    {
        using var _ = _loadingService.BeginScope("Gerando URL de autorização...");
        try
        {
            var client = _httpClientFactory.CreateClient("Orquestrador");
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                BuildAuthorizeRelativeUrl(platformType, redirectUri, state, friendlyName, lojaId));

            await ApplyBearerAsync(request, cancellationToken);

            using var response = await client.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return (false, null, $"Não foi possível gerar a URL de autorização ({(int)response.StatusCode}).");
            }

            var payload = TryDeserialize<MarketplaceAuthorizeUrlResponseDto>(content);
            var authorizeUrl = FirstNonEmpty(payload?.AuthorizeUrl, payload?.AuthorizationUrl);
            return string.IsNullOrWhiteSpace(authorizeUrl)
                ? (false, null, "A API não retornou a URL de autorização.")
                : (true, authorizeUrl, null);
        }
        catch (Exception)
        {
            return (false, null, "Não foi possível contactar a API para iniciar OAuth.");
        }
    }

    public static string BuildAuthorizeRelativeUrl(
        MarketplaceType platformType,
        string redirectUri,
        string? state = null,
        string? friendlyName = null,
        string? lojaId = null)
    {
        var slug = MarketplacePlatformRoute.ToSlug(platformType);
        var url =
            $"api/marketplace-auth/{slug}/authorize-url?redirectUri={Uri.EscapeDataString(redirectUri)}";

        if (!string.IsNullOrWhiteSpace(state))
        {
            url += $"&state={Uri.EscapeDataString(state)}";
        }

        if (!string.IsNullOrWhiteSpace(friendlyName))
        {
            url += $"&friendlyName={Uri.EscapeDataString(friendlyName)}";
        }

        if (!string.IsNullOrWhiteSpace(lojaId))
        {
            url += $"&lojaId={Uri.EscapeDataString(lojaId)}";
        }

        return url;
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private async Task<IntegracaoLojaResponseDto> SendEnvelopeAsync(
        HttpMethod method,
        string relativeUrl,
        object? body,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Orquestrador");
            using var request = new HttpRequestMessage(method, relativeUrl);

            await ApplyBearerAsync(request, cancellationToken);

            if (body is not null)
            {
                request.Content = JsonContent.Create(body, body.GetType(), options: JsonOptions);
            }

            using var response = await client.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(content))
            {
                return new IntegracaoLojaResponseDto
                {
                    Status = false,
                    Descricao = HttpService.FormatIisConnectionError(response.StatusCode)
                };
            }

            var envelope = TryDeserializeEnvelope(content);

            if (envelope is not null)
            {
                if (!envelope.Status && string.IsNullOrWhiteSpace(envelope.Descricao))
                {
                    envelope.Descricao = Truncate(content);
                }

                return envelope;
            }

            _logger.LogWarning(
                "Resposta de integrações não interpretada. Status={StatusCode} Payload={Payload}",
                (int)response.StatusCode,
                Truncate(content, 200));

            return new IntegracaoLojaResponseDto
            {
                Status = false,
                Descricao = HttpService.FormatIisError(content)
            };
        }
        catch (TaskCanceledException)
        {
            return new IntegracaoLojaResponseDto
            {
                Status = false,
                Descricao = "Tempo limite excedido ao contactar o servidor."
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Falha HTTP ao chamar integrações {Method} {Url}.", method, relativeUrl);
            return new IntegracaoLojaResponseDto
            {
                Status = false,
                Descricao = HttpService.FormatIisConnectionError(ex.StatusCode)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha HTTP ao chamar integrações {Method} {Url}.", method, relativeUrl);
            return new IntegracaoLojaResponseDto
            {
                Status = false,
                Descricao = string.IsNullOrWhiteSpace(ex.Message)
                    ? "Não foi possível contactar o servidor. Verifique se a API está em execução."
                    : ex.Message
            };
        }
    }

    private async Task ApplyBearerAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var accessToken = await _accessTokenProvider.GetAccessTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            _logger.LogWarning(
                "JWT ausente ao chamar {Method} {Url}. A API deve responder 401.",
                request.Method,
                request.RequestUri);
            return;
        }

        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken.Trim());
    }

    private IntegracaoLojaResponseDto? TryDeserializeEnvelope(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var root = document.RootElement;
            if (root.TryGetProperty("status", out var statusElement)
                && statusElement.ValueKind is JsonValueKind.Number)
            {
                return new IntegracaoLojaResponseDto
                {
                    Status = false,
                    Descricao = ReadString(root, "descricao")
                        ?? ReadString(root, "detail")
                        ?? ReadString(root, "title")
                        ?? ReadString(root, "error")
                        ?? ReadString(root, "message")
                        ?? HttpService.FormatIisError(json)
                };
            }

            return JsonSerializer.Deserialize<IntegracaoLojaResponseDto>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Falha ao desserializar payload de integrações. Payload={Payload}", Truncate(json));
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao desserializar payload de integrações. Payload={Payload}", Truncate(json));
            return null;
        }
    }

    private static string? ReadString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() : property.ToString();
    }

    private static string Truncate(string? value, int maxLength = 2000)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value ?? string.Empty;
        }

        return value[..maxLength];
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
