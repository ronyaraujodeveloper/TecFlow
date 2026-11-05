using System.ComponentModel.DataAnnotations;

namespace Ofertio.Business.Dto;

public class NewCampaignDto
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    [Range(typeof(decimal), "0", "1000000", ErrorMessage = "Orçamento inválido. Informe um valor de 0 a 1.000.000,00.")]
    public decimal Budget { get; set; }
    public required string Description { get; set; }
}
