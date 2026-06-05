namespace TecFlow.Business.Dto;

public class AffiliateDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AffiliateCode { get; set; } = string.Empty;
    public decimal Commission { get; set; }
    public int CampaignId { get; set; }
    public int? ContentId { get; set; }
}
