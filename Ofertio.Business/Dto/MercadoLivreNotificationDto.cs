using System.Text.Json.Serialization;

namespace Ofertio.Business.Dto;

/// <summary>Notificação POST do Mercado Livre (tópicos orders_v2, items, questions, messages...).</summary>
public sealed class MercadoLivreNotificationDto
{
    [JsonPropertyName("_id")]
    public string? Id { get; set; }

    /// <summary>Recurso a consultar na API do ML (ex.: <c>/orders/2000000000</c>).</summary>
    [JsonPropertyName("resource")]
    public string? Resource { get; set; }

    [JsonPropertyName("user_id")]
    public long? UserId { get; set; }

    [JsonPropertyName("topic")]
    public string? Topic { get; set; }

    [JsonPropertyName("application_id")]
    public long? ApplicationId { get; set; }

    [JsonPropertyName("attempts")]
    public int? Attempts { get; set; }

    [JsonPropertyName("sent")]
    public DateTimeOffset? Sent { get; set; }

    [JsonPropertyName("received")]
    public DateTimeOffset? Received { get; set; }
}
