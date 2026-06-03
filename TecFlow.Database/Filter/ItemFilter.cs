namespace TecFlow.Database.Filter;

public class ItemFilter
{
    public int? Id { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? PopularityScore { get; set; }
    public int? OwnerId { get; set; }
}
