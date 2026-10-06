namespace TecFlow.Business.Dto;

public class OfferDataExtraction
{
    public string ProductTitle { get; set; } = string.Empty;

    public decimal? Price { get; set; }

    public string CouponCode { get; set; } = string.Empty;

    public string PrimaryProductUrl { get; set; } = string.Empty;

    public string Platform { get; set; } = string.Empty;
}
