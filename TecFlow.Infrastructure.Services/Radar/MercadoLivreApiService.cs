using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Enums;

namespace TecFlow.Infrastructure.Services.Radar;

public sealed class MercadoLivreApiService : IMercadoLivreApiService
{
    public const string PublicUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    private static readonly JsonSerializerOptions SearchJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<MercadoLivreApiService> _logger;

    public MercadoLivreApiService(HttpClient httpClient, ILogger<MercadoLivreApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        EnsurePublicUserAgent(_httpClient);
    }

    public async Task<OfficialOfferSnapshotDto?> GetItemAsync(string itemId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://api.mercadolibre.com/items/{Uri.EscapeDataString(itemId.Trim())}");
            ApplyBrowserHeaders(request);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Erro API ML [{StatusCode}]: {Body}", response.StatusCode, json);
                return null;
            }

            return MercadoLivreItemParser.Parse(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exceção ao consultar item Mercado Livre '{Item}'", itemId);
            return null;
        }
    }

    public async Task<OfficialCatalogChannelResult> SearchProductsAsync(
        string query,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return OfficialCatalogChannelResult.Empty;
        }

        query = query.Trim();
        _ = limit;
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://api.mercadolibre.com/sites/MLB/search?q={Uri.EscapeDataString(query)}&limit=20");
            ApplyBrowserHeaders(request);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Erro API ML [{StatusCode}]: {Body}", response.StatusCode, body);
                return OfficialCatalogChannelResult.Failed(
                    $"Erro HTTP {(int)response.StatusCode} ({response.StatusCode})");
            }

            MercadoLivreSearchResponse? payload;
            try
            {
                payload = JsonSerializer.Deserialize<MercadoLivreSearchResponse>(body, SearchJsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Exceção ao buscar no Mercado Livre para query '{Query}'", query);
                throw;
            }

            if (payload?.Results == null || payload.Results.Count == 0)
            {
                return OfficialCatalogChannelResult.Empty;
            }

            var items = payload.Results
                .Select(item => new OfficialCatalogProductDto
                {
                    Platform = "Mercado Livre",
                    PlatformType = MarketplaceType.MercadoLivre,
                    ProductId = string.IsNullOrWhiteSpace(item.Id) ? null : item.Id,
                    ProductName = string.IsNullOrWhiteSpace(item.Title) ? null : item.Title,
                    Price = item.Price,
                    OriginalPrice = item.OriginalPrice,
                    ImageUrl = string.IsNullOrWhiteSpace(item.SecureThumbnail)
                        ? item.Thumbnail?.Replace("http://", "https://", StringComparison.OrdinalIgnoreCase)
                        : item.SecureThumbnail.Replace("http://", "https://", StringComparison.OrdinalIgnoreCase),
                    SourceUrl = string.IsNullOrWhiteSpace(item.Permalink)
                        ? string.Empty
                        : item.Permalink,
                    Shipping = item.Shipping?.FreeShipping == true ? "Frete grátis" : null,
                    Source = "Api"
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.SourceUrl))
                .ToList();

            return OfficialCatalogChannelResult.From(items);
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exceção ao buscar no Mercado Livre para query '{Query}'", query);
            throw;
        }
    }

    internal static void EnsurePublicUserAgent(HttpClient httpClient)
    {
        httpClient.DefaultRequestHeaders.UserAgent.Clear();
        httpClient.DefaultRequestHeaders.Remove("User-Agent");
        ApplyHeader(httpClient.DefaultRequestHeaders, "User-Agent", PublicUserAgent);
        if (!httpClient.DefaultRequestHeaders.Accept.Any(item => item.MediaType == "application/json"))
        {
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
    }

    private static void ApplyBrowserHeaders(HttpRequestMessage request)
    {
        ApplyHeader(request.Headers, "User-Agent", PublicUserAgent);
        ApplyHeader(request.Headers, "Accept", "application/json");
        ApplyHeader(request.Headers, "Accept-Language", "pt-BR,pt;q=0.9,en-US;q=0.8");
    }

    private static void ApplyHeader(HttpHeaders headers, string name, string value)
    {
        headers.Remove(name);
        if (!headers.TryAddWithoutValidation(name, value))
        {
            headers.Add(name, value);
        }
    }
}
