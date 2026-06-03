using System.ComponentModel.DataAnnotations;

namespace TecFlow.Business.Dto;

public class NewCampaignDto
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Budget { get; set; }
    public required string Description { get; set; }
}
