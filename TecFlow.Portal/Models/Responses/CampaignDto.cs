namespace TecFlow.Portal.Models.Responses;

public class CampaignDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Budget { get; set; }
    public int OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool IsActive(DateTime? referenceUtc = null)
    {
        var now = referenceUtc ?? DateTime.UtcNow;
        return StartDate <= now && EndDate >= now;
    }
}
