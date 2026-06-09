using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TecFlow.Business.Integrations.Common;
using TecFlow.Business.Integrations.TikTokShop;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Database.Entity;

namespace TecFlow.Infrastructure.Services.LinkStrategies;

/// <summary>Integração com endpoint de geração de links do programa de afiliados TikTok Shop.</summary>
public sealed class TikTokAffiliateLinkClient : ITikTokAffiliateLinkClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly TikTokShopIntegrationOptions _options;
    private readonly ILogger<TikTokAffiliateLinkClient> _logger;

    public TikTokAffiliateLinkClient(
        HttpClient httpClient,
        IOptions<TikTokShopIntegrationOptions> options,
        ILogger<TikTokAffiliateLinkClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GenerateAffiliateLinkAsync(
        IntegracaoLoja store,
        string expandedProductUrl,
        string affiliateId,
        string? customNickname,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.AppKey) || string.IsNullOrWhiteSpace(_options.AppSecret))
        {
            throw new AffiliateLinkGenerationException(
                "Credenciais TikTok Shop não configuradas. Contacte o administrador do sistema.");
        }

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var path = _options.GenerateAffiliateLinkPath;
        var payload = new
        {
            product_url = expandedProductUrl,
            tracking_id = affiliateId,
            shop_id = store.ShopId,
            sub_id = customNickname,
            access_token = store.AccessToken
        };

        var jsonBody = JsonSerializer.Serialize(payload, JsonOptions);
        var sign = MarketplaceSignatureHelper.GenerateTikTokShopSign(
            _options.AppKey,
            _options.AppSecret,
            path,
            timestamp,
            jsonBody);

        var query =
            $"?app_key={Uri.EscapeDataString(_options.AppKey)}&timestamp={timestamp}&sign={sign}&shop_id={Uri.EscapeDataString(store.ShopId)}";
        var requestUri = $"{path.TrimStart('/')}{query}";

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        request.Headers.TryAddWithoutValidation("x-tts-access-token", store.AccessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "TikTok Shop affiliate link falhou. Status={StatusCode}. Body={Body}",
                (int)response.StatusCode,
                content);

            throw MapTikTokFailure(content, response.StatusCode);
        }

        var link = TryExtractAffiliateUrl(content);
        if (string.IsNullOrWhiteSpace(link))
        {
            throw new AffiliateLinkGenerationException(
                "O TikTok Shop não retornou um link de afiliado válido. Verifique se o produto participa do programa.");
        }

        return link;
    }

    private static AffiliateLinkGenerationException MapTikTokFailure(
        string content,
        System.Net.HttpStatusCode statusCode)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("message", out var messageProp))
            {
                var message = messageProp.GetString();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    return new AffiliateLinkGenerationException(
                        $"TikTok Shop recusou a geração do link: {message}");
                }
            }
        }
        catch (JsonException)
        {
            // Ignorado.
        }

        return statusCode == System.Net.HttpStatusCode.NotFound
            ? new AffiliateLinkGenerationException(
                "Produto não encontrado ou indisponível no programa de afiliados TikTok Shop.")
            : new AffiliateLinkGenerationException(
                "Não foi possível gerar o link de afiliado TikTok Shop. Verifique a URL e o status da loja.");
    }

    private static string? TryExtractAffiliateUrl(string content)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.TryGetProperty("data", out var data)
                && data.TryGetProperty("affiliate_link", out var affiliateLink))
            {
                return affiliateLink.GetString();
            }

            if (root.TryGetProperty("data", out data)
                && data.TryGetProperty("affiliateLink", out var camelLink))
            {
                return camelLink.GetString();
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }
}
