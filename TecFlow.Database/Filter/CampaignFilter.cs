namespace TecFlow.Database.Filter;

public class CampaignFilter
{
    public int? Id { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? Budget { get; set; }
    public int? OwnerId { get; set; }
}
