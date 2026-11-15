using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Ofertio.Business.Dto;

namespace Ofertio.SharedUi.Services.Public;

public interface IPublicPlansApiService
{
    /// <summary>Planos ativos e regras do teste grátis. Devolve nulo se a API estiver fora do ar.</summary>
    Task<PublicPlansDto?> GetAsync(CancellationToken cancellationToken = default);
}

public sealed class PublicPlansApiService : IPublicPlansApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PublicPlansApiService> _logger;

    public PublicPlansApiService(IHttpClientFactory httpClientFactory, ILogger<PublicPlansApiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<PublicPlansDto?> GetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Orquestrador");
            return await client.GetFromJsonAsync<PublicPlansDto>("api/public/plans", cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            _logger.LogWarning(ex, "Não foi possível carregar os planos públicos.");
            return null;
        }
    }
}