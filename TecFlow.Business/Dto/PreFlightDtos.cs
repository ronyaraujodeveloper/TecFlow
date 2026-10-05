namespace TecFlow.Business.Dto;

public class PreFlightNotificationDto
{
    public int Id { get; set; }

    public string Channel { get; set; } = string.Empty;

    public int CampaignId { get; set; }

    public string? CampaignTitle { get; set; }

    public string? ProductUrl { get; set; }

    public string? CouponCode { get; set; }

    public string AlertType { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public decimal? OriginalPrice { get; set; }

    public decimal? CurrentPrice { get; set; }

    public string? SubstituteUrl { get; set; }

    public decimal? SubstitutePrice { get; set; }

    public string? SubstituteName { get; set; }

    public bool IsResolved { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class PreFlightNotificationResponseDto
{
    public bool Status { get; set; } = true;

    public string Descricao { get; set; } = string.Empty;

    public List<PreFlightNotificationDto> Items { get; set; } = [];
}
