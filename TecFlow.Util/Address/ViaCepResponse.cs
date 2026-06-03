using System.Text.Json.Serialization;

namespace TecFlow.Util.Address;

/// <summary>
/// Payload retornado pela API pública do ViaCEP.
/// </summary>
internal sealed class ViaCepResponse
{
    [JsonPropertyName("cep")]
    public string? ZipCode { get; set; }

    [JsonPropertyName("logradouro")]
    public string? Street { get; set; }

    [JsonPropertyName("bairro")]
    public string? Neighborhood { get; set; }

    [JsonPropertyName("localidade")]
    public string? City { get; set; }

    [JsonPropertyName("uf")]
    public string? State { get; set; }

    [JsonPropertyName("erro")]
    public bool NotFound { get; set; }
}
