using System.ComponentModel.DataAnnotations.Schema;

namespace TecFlow.Core.Entities;

[Table("Conversaos")]
public class Conversion : BaseEntity
{
    [Column("AfiliadoId")]
    public int AffiliateId { get; set; }

    [Column("ValorVenda")]
    public decimal SaleAmount { get; set; }

    [Column("Cliques")]
    public int Clicks { get; set; }

    [Column("Vendas")]
    public int Sales { get; set; }

    public int OwnerId { get; set; }
    public UserAccount? Owner { get; set; }
}
