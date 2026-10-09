using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;

namespace TecFlow.Infrastructure.Services.Radar;

public sealed class MercadoLivreApiService : IMercadoLivreApiService
{
    public const string PublicUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) TecFlowApp/1.0";

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
            using var response = await _httpClient.GetAsync($"items/{Uri.EscapeDataString(itemId.Trim())}", cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Erro na API ML: {status}", response.StatusCode);
                return null;
            }

            return MercadoLivreItemParser.Parse(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao consultar Mercado Livre. Item={Item}", itemId);
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
            using var response = await _httpClient.GetAsync(
                $"https://api.mercadolibre.com/sites/MLB/search?q={Uri.EscapeDataString(query)}&limit=20",
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Erro na API ML: {status}", response.StatusCode);
                return OfficialCatalogChannelResult.Empty;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return OfficialCatalogChannelResult.From(MercadoLivreItemParser.ParseSearch(json));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha na busca Mercado Livre. Query={Query}", query);
            return OfficialCatalogChannelResult.Empty;
        }
    }

    internal static void EnsurePublicUserAgent(HttpClient httpClient)
    {
        httpClient.DefaultRequestHeaders.UserAgent.Clear();
        httpClient.DefaultRequestHeaders.Remove("User-Agent");
        try
        {
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(PublicUserAgent);
        }
        catch (FormatException)
        {
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", PublicUserAgent);
        }

        if (!httpClient.DefaultRequestHeaders.Accept.Any(item => item.MediaType == "application/json"))
        {
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
    }
}
