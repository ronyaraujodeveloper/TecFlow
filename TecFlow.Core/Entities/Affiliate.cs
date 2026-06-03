using System.ComponentModel.DataAnnotations.Schema;

namespace TecFlow.Core.Entities;

[Table("Afiliados")]
public class Affiliate : BaseEntity
{
    [Column("Nome")]
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    [Column("CodigoAfiliado")]
    public string AffiliateCode { get; set; } = Guid.NewGuid().ToString();

    [Column("Comissao")]
    public decimal Commission { get; set; }

    [Column("CampanhaId")]
    public int CampaignId { get; set; }

    public Campaign? Campaign { get; set; }

    [Column("ConteudoId")]
    public int? ContentId { get; set; }

    public Content? Content { get; set; }

    public int OwnerId { get; set; }
    public UserAccount? Owner { get; set; }
}
