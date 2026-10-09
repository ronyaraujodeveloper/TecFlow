using System.Text.Json.Serialization;

namespace TecFlow.Business.Dto;

public class MercadoLivreSearchResponse
{
    [JsonPropertyName("results")]
    public List<MercadoLivreItemDto> Results { get; set; } = new();
}

public class MercadoLivreItemDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("permalink")]
    public string Permalink { get; set; } = string.Empty;

    [JsonPropertyName("thumbnail")]
    public string Thumbnail { get; set; } = string.Empty;

    [JsonPropertyName("secure_thumbnail")]
    public string? SecureThumbnail { get; set; }

    [JsonPropertyName("original_price")]
    public decimal? OriginalPrice { get; set; }

    [JsonPropertyName("shipping")]
    public MercadoLivreShippingDto? Shipping { get; set; }
}

public class MercadoLivreShippingDto
{
    [JsonPropertyName("free_shipping")]
    public bool FreeShipping { get; set; }
}
