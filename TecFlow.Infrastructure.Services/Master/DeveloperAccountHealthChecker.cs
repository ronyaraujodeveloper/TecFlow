using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Master;
using TecFlow.Infrastructure.Services.Radar;

namespace TecFlow.Infrastructure.Services.Master;

/// <summary>Diagnóstico direto das credenciais de app contra os servidores de cada marketplace.</summary>
public sealed class DeveloperAccountHealthChecker : IDeveloperAccountHealthChecker
{
    public const string MercadoLivreTokenUrl = "https://api.mercadolibre.com/oauth/token";
    public const string ShopeeAffiliateGraphQlUrl = "https://open-api.affiliate.shopee.com.br/graphql";

    private readonly HttpClient _http;
    private readonly ILogger<DeveloperAccountHealthChecker> _logger;

    public DeveloperAccountHealthChecker(HttpClient http, ILogger<DeveloperAccountHealthChecker> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<DeveloperCredentialCheck> CheckAsync(
        string platform,
        string? clientId,
        string? secret,
        string? extra,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(secret))
        {
            return new DeveloperCredentialCheck(false, "Credenciais não configuradas (ID ou segredo vazio).", 0);
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var (success, message) = DeveloperPlatforms.Normalize(platform) switch
            {
                DeveloperPlatforms.MercadoLivre => await CheckMercadoLivreAsync(clientId.Trim(), secret.Trim(), cancellationToken),
                DeveloperPlatforms.Shopee => await CheckShopeeAsync(clientId.Trim(), secret.Trim(), cancellationToken),
                DeveloperPlatforms.Amazon => await CheckAmazonAsync(clientId.Trim(), secret.Trim(), extra?.Trim(), cancellationToken),
                _ => (false, "Plataforma não suportada.")
            };
            return new DeveloperCredentialCheck(success, message, stopwatch.ElapsedMilliseconds);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new DeveloperCredentialCheck(false, "Tempo esgotado aguardando o servidor do marketplace.", stopwatch.ElapsedMilliseconds);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Falha de rede no diagnóstico de {Platform}.", platform);
            return new DeveloperCredentialCheck(false, "Falha de rede: " + ex.Message, stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task<(bool, string)> CheckMercadoLivreAsync(string appId, string secret, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, MercadoLivreTokenUrl)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = appId,
                ["client_secret"] = secret
            })
        };
        request.Headers.Accept.ParseAdd("application/json");
        using var response = await _http.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var hours = ReadNumber(json, "expires_in") is { } seconds && seconds > 0 ? seconds / 3600d : 6d;
            return (true, $"Token de app emitido pelo Mercado Livre (válido por {hours.ToString("0.#", CultureInfo.InvariantCulture)}h).");
        }

        var reason = ReadString(json, "message") ?? ReadString(json, "error") ?? response.ReasonPhrase;
        return (false, $"Mercado Livre recusou as credenciais (HTTP {(int)response.StatusCode}): {reason}");
    }

    private async Task<(bool, string)> CheckShopeeAsync(string appId, string secret, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new
        {
            query = "{ productOfferV2(keyword: \"celular\", page: 1, limit: 1) { nodes { itemId } } }"
        });
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        using var request = new HttpRequestMessage(HttpMethod.Post, ShopeeAffiliateGraphQlUrl)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("Authorization", BuildShopeeAuthorization(appId, secret, timestamp, payload));
        using var response = await _http.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var error = ReadGraphQlError(json);
        if (response.IsSuccessStatusCode && error is null)
        {
            return (true, "API de Afiliados da Shopee respondeu com assinatura válida.");
        }

        return (false, $"Shopee recusou a requisição (HTTP {(int)response.StatusCode}): {error ?? response.ReasonPhrase}");
    }

    /// <summary>Formato oficial da Shopee Affiliate Open API: Signature = SHA256(AppId + Timestamp + Payload + Secret).</summary>
    public static string BuildShopeeAuthorization(string appId, string secret, long timestamp, string payload)
    {
        var raw = appId + timestamp.ToString(CultureInfo.InvariantCulture) + payload + secret;
        var signature = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
        return $"SHA256 Credential={appId}, Timestamp={timestamp}, Signature={signature}";
    }

    private async Task<(bool, string)> CheckAmazonAsync(string accessKey, string secretKey, string? partnerTag, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(partnerTag))
        {
            return (false, "Informe o Partner Tag (ex.: minhaloja-20) para testar a PA-API.");
        }

        var payload = JsonSerializer.Serialize(new
        {
            PartnerTag = partnerTag,
            PartnerType = "Associates",
            Marketplace = "www.amazon.com.br",
            Keywords = "livro",
            SearchIndex = "All",
            ItemCount = 1,
            Resources = new[] { "ItemInfo.Title" }
        });
        using var request = AmazonPaApiService.CreateSignedRequest(
            accessKey,
            secretKey,
            AmazonPaApiService.DefaultHost,
            AmazonPaApiService.DefaultRegion,
            payload,
            "/paapi5/searchitems",
            "com.amazon.paapi5.v1.ProductAdvertisingAPIv1.SearchItems",
            DateTime.UtcNow);
        using var response = await _http.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return (true, "PA-API da Amazon respondeu com assinatura válida.");
        }

        var (code, message) = ReadAmazonError(json);
        if (response.StatusCode == HttpStatusCode.TooManyRequests || code == "TooManyRequests")
        {
            return (true, "Credenciais aceitas pela Amazon (limite de requisições atingido no momento).");
        }

        return (false, $"Amazon recusou a requisição (HTTP {(int)response.StatusCode}): {code} {message}".TrimEnd());
    }

    private static double? ReadNumber(string json, string property)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number
                ? value.GetDouble()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ReadString(string json, string property)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.Object
                   && document.RootElement.TryGetProperty(property, out var value)
                   && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    internal static string? ReadGraphQlError(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("errors", out var errors)
                || errors.ValueKind != JsonValueKind.Array
                || errors.GetArrayLength() == 0)
            {
                return null;
            }

            var first = errors[0];
            if (first.TryGetProperty("extensions", out var extensions))
            {
                var code = extensions.TryGetProperty("code", out var codeValue) ? codeValue.ToString() : null;
                var message = extensions.TryGetProperty("message", out var messageValue) ? messageValue.GetString() : null;
                if (!string.IsNullOrWhiteSpace(message))
                {
                    return string.IsNullOrWhiteSpace(code) ? message : $"{code} {message}";
                }
            }

            return first.TryGetProperty("message", out var plain) ? plain.GetString() : "Erro desconhecido.";
        }
        catch (JsonException)
        {
            return "Resposta inválida do servidor.";
        }
    }

    private static (string? Code, string? Message) ReadAmazonError(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("Errors", out var errors)
                && errors.ValueKind == JsonValueKind.Array
                && errors.GetArrayLength() > 0)
            {
                var first = errors[0];
                return (
                    first.TryGetProperty("Code", out var code) ? code.GetString() : null,
                    first.TryGetProperty("Message", out var message) ? message.GetString() : null);
            }
        }
        catch (JsonException)
        {
        }

        return (null, null);
    }
}
