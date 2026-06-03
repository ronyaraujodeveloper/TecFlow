using System.Net.Http.Json;
using System.Text.Json;
using TecFlow.Util.Validation;

namespace TecFlow.Util.Address;

/// <summary>
/// Consulta endereços por CEP via API pública do ViaCEP.
/// </summary>
public sealed class CepService : ICepService
{
    private readonly HttpClient _httpClient;

    public CepService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<CepResultDto?> SearchCepAsync(string cep, CancellationToken cancellationToken = default)
    {
        var normalizedCep = ValidationHelper.NormalizeCepDigits(cep);
        if (normalizedCep is null)
        {
            return null;
        }

        try
        {
            var response = await _httpClient.GetFromJsonAsync<ViaCepResponse>(
                $"{normalizedCep}/json/",
                cancellationToken);

            if (response is null || response.NotFound)
            {
                return null;
            }

            return new CepResultDto
            {
                ZipCode = response.ZipCode ?? normalizedCep,
                Street = response.Street ?? string.Empty,
                Neighborhood = response.Neighborhood ?? string.Empty,
                City = response.City ?? string.Empty,
                State = response.State ?? string.Empty
            };
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
