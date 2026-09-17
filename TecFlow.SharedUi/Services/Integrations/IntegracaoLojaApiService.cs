using System.Net.Http.Json;
using System.Text.Json;
using TecFlow.Business.Dto;
using TecFlow.Business.Integrations.Auth;
using TecFlow.Core.Enums;
using TecFlow.Database.Filter;
using TecFlow.SharedUi.Extensions;
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
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAccessTokenProvider _accessTokenProvider;
    private readonly ILoadingService _loadingService;

    public IntegracaoLojaApiService(
        IHttpClientFactory httpClientFactory,
        IAccessTokenProvider accessTokenProvider,
        ILoadingService loadingService)
    {
        _httpClientFactory = httpClientFactory;
        _accessTokenProvider = accessTokenProvider;
        _loadingService = loadingService;
    }

    public Task<IntegracaoLojaResponseDto> ListAsync(
        IntegracaoLojaFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        using var _ = _loadingService.BeginScope("Carregando lojas integradas...");
        filter ??= new IntegracaoLojaFilter { Page = 1, PageSize = 100 };
        var url = "api/integracoes/lojas".AppendQueryString(filter);
        return SendEnvelopeAsync(HttpMethod.Get, url, null, cancellationToken);
    }

    public Task<IntegracaoLojaResponseDto> LinkAsync(
        IntegracaoLojaDto request,
        CancellationToken cancellationToken = default)
    {
        using var _ = _loadingService.BeginScope("Vinculando nova loja...");
        return SendEnvelopeAsync(HttpMethod.Post, "api/integracoes/vincular", request, cancellationToken);
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

            var accessToken = _accessTokenProvider.GetAccessToken();
            if (!string.IsNullOrEmpty(accessToken))
            {
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            }

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
            var envelope = TryDeserialize<IntegracaoLojaResponseDto>(content);

            if (envelope is not null)
            {
                return envelope;
            }

            return new IntegracaoLojaResponseDto
            {
                Status = false,
                Descricao = "Não foi possível interpretar a resposta do servidor."
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
        catch (HttpRequestException)
        {
            return new IntegracaoLojaResponseDto
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
