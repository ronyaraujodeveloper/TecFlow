using Microsoft.Extensions.Logging;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Radar;

namespace TecFlow.Infrastructure.Services.Radar;

public sealed class MercadoLivreApiService : IMercadoLivreApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<MercadoLivreApiService> _logger;

    public MercadoLivreApiService(HttpClient http, ILogger<MercadoLivreApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<OfficialOfferSnapshotDto?> GetItemAsync(string itemId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return null;
        }

        try
        {
            using var response = await _http.GetAsync($"items/{Uri.EscapeDataString(itemId.Trim())}", cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Mercado Livre /items falhou. Status={Status} Item={Item}", (int)response.StatusCode, itemId);
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

    public async Task<IReadOnlyList<OfficialCatalogProductDto>> SearchProductsAsync(
        string query,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        try
        {
            var size = OfficialCatalogSearchRules.ClampLimit(limit);
            var path = $"sites/MLB/search?q={Uri.EscapeDataString(query.Trim())}&limit={size}";
            using var response = await _http.GetAsync(path, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Mercado Livre /search falhou. Status={Status}", (int)response.StatusCode);
                return [];
            }

            return MercadoLivreItemParser.ParseSearch(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha na busca Mercado Livre. Query={Query}", query);
            return [];
        }
    }
}
